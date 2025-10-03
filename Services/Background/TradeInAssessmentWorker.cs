using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Services.AI;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AccessoryWorld.Services.Background
{
    /// <summary>
    /// Background worker for processing trade-in AI assessments
    /// </summary>
    public sealed class TradeInAssessmentWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITradeInQueue _queue;
        private readonly ILogger<TradeInAssessmentWorker> _logger;
        private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

        public TradeInAssessmentWorker(
            IServiceProvider serviceProvider,
            ITradeInQueue queue,
            ILogger<TradeInAssessmentWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _queue = queue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("TradeInAssessmentWorker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var tradeInId = await _queue.DequeueAsync(stoppingToken);
                    
                    if (tradeInId.HasValue)
                    {
                        await ProcessTradeInAsync(tradeInId.Value, stoppingToken);
                    }
                    else
                    {
                        // No items in queue, wait before checking again
                        await Task.Delay(_pollingInterval, stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in TradeInAssessmentWorker main loop");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Wait before retrying
                }
            }

            _logger.LogInformation("TradeInAssessmentWorker stopped");
        }

        private async Task ProcessTradeInAsync(int tradeInId, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var assessmentProvider = scope.ServiceProvider.GetRequiredService<IDeviceAssessmentProvider>();
            var pricingService = scope.ServiceProvider.GetRequiredService<PricingService>();

            try
            {
                _logger.LogInformation("Processing TradeIn {TradeInId}", tradeInId);

                // Get trade-in with photos
                var tradeIn = await context.TradeIns
                    .FirstOrDefaultAsync(t => t.Id == tradeInId, cancellationToken);

                if (tradeIn == null)
                {
                    _logger.LogWarning("TradeIn {TradeInId} not found", tradeInId);
                    return;
                }

                // Check if already processed
                if (!string.IsNullOrEmpty(tradeIn.AutoGrade))
                {
                    _logger.LogInformation("TradeIn {TradeInId} already has AI assessment", tradeInId);
                    return;
                }

                // Update status to processing
                tradeIn.Status = "AI_PROCESSING";
                await context.SaveChangesAsync(cancellationToken);

                // Get photo URLs
                var photoUrls = GetPhotoUrls(tradeIn);
                if (!photoUrls.Any())
                {
                    _logger.LogWarning("TradeIn {TradeInId} has no photos for analysis", tradeInId);
                    await UpdateTradeInWithError(context, tradeIn, "No photos available for analysis");
                    return;
                }

                // Perform AI assessment
                var assessmentRequest = new DeviceAssessmentRequest
                {
                    ImageUrls = photoUrls,
                    DeviceBrand = tradeIn.DeviceBrand,
                    DeviceModel = tradeIn.DeviceModel,
                    DeviceType = "smartphone" // Default, could be enhanced
                };

                var assessment = await assessmentProvider.AnalyzeAsync(assessmentRequest, cancellationToken);

                // Calculate pricing
                var quote = await pricingService.QuoteAsync(tradeInId, assessment, cancellationToken);

                // Update trade-in with results
                tradeIn.AiVendor = assessmentProvider.ProviderName;
                tradeIn.AiVersion = assessmentProvider.ModelVersion;
                tradeIn.AiAssessmentJson = JsonSerializer.Serialize(assessment);
                tradeIn.AiConfidence = (float)assessment.IdentificationConfidence;
                tradeIn.AutoGrade = quote.Grade;
                tradeIn.AutoOfferAmount = quote.OfferAmount;
                tradeIn.AutoOfferBreakdownJson = quote.Breakdown.ToJson();
                tradeIn.Status = quote.IsAcceptable ? "AI_ASSESSED" : "AI_REJECTED";

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Successfully processed TradeIn {TradeInId}: Grade {Grade}, Offer {Offer:C}", 
                    tradeInId, quote.Grade, quote.OfferAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process TradeIn {TradeInId}", tradeInId);
                
                // Update trade-in with error status
                var tradeIn = await context.TradeIns
                    .FirstOrDefaultAsync(t => t.Id == tradeInId, cancellationToken);
                
                if (tradeIn != null)
                {
                    // Check if this is a retryable error and we haven't exceeded max retries
                    var retryCount = tradeIn.AiRetryCount;
                    const int maxRetries = 3;
                    
                    if (IsRetryableError(ex) && retryCount < maxRetries)
                    {
                        // Increment retry count and re-queue for processing
                        tradeIn.AiRetryCount = retryCount + 1;
                        tradeIn.Status = "SUBMITTED"; // Reset to allow retry
                        await context.SaveChangesAsync(cancellationToken);
                        
                        // Re-queue with exponential backoff delay
                        var delayMinutes = Math.Pow(2, retryCount) * 5; // 5, 10, 20 minutes
                        await _queue.EnqueueAsync(tradeInId, 2, (int)delayMinutes);
                        
                        _logger.LogInformation("TradeIn {TradeInId} queued for retry {RetryCount}/{MaxRetries} with {DelayMinutes} minute delay", 
                            tradeInId, retryCount + 1, maxRetries, delayMinutes);
                    }
                    else
                    {
                        // Max retries exceeded or non-retryable error
                        await UpdateTradeInWithError(context, tradeIn, ex.Message);
                        _logger.LogWarning("TradeIn {TradeInId} failed permanently after {RetryCount} retries", tradeInId, retryCount);
                    }
                }
            }
        }

        private static List<string> GetPhotoUrls(TradeIn tradeIn)
        {
            if (string.IsNullOrEmpty(tradeIn.PhotosJson))
                return new List<string>();

            try
            {
                var photos = JsonSerializer.Deserialize<List<string>>(tradeIn.PhotosJson);
                return photos ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static async Task UpdateTradeInWithError(
            ApplicationDbContext context, 
            TradeIn tradeIn, 
            string errorMessage)
        {
            tradeIn.Status = "AI_ERROR";
            tradeIn.AiAssessmentJson = JsonSerializer.Serialize(new { error = errorMessage });
            await context.SaveChangesAsync();
        }

        private static bool IsRetryableError(Exception ex)
        {
            // Determine if the error is worth retrying
            return ex switch
            {
                HttpRequestException => true,           // Network issues
                TaskCanceledException => true,         // Timeout issues
                InvalidOperationException when ex.Message.Contains("BaseAddress") => true, // HTTP client config issues
                _ when ex.Message.Contains("timeout") => true,
                _ when ex.Message.Contains("connection") => true,
                _ when ex.Message.Contains("network") => true,
                _ => false // Don't retry validation errors, auth errors, etc.
            };
        }
    }
}