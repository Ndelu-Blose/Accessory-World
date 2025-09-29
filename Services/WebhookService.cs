using AccessoryWorld.Data;
using AccessoryWorld.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AccessoryWorld.Services
{
    public interface IWebhookService
    {
        Task<bool> ProcessWebhookAsync(string eventId, string eventType, string source, object payload, Func<Task<bool>> processor);
        Task<WebhookEvent?> GetWebhookEventAsync(string eventId);
        Task<bool> IsWebhookProcessedAsync(string eventId);
        Task MarkWebhookAsProcessedAsync(string eventId, string result);
        Task MarkWebhookAsFailedAsync(string eventId, string errorMessage);
        Task<List<WebhookEvent>> GetFailedWebhooksForRetryAsync();
        Task RetryFailedWebhookAsync(int webhookEventId);
    }

    public class WebhookService : IWebhookService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WebhookService> _logger;

        public WebhookService(ApplicationDbContext context, ILogger<WebhookService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> ProcessWebhookAsync(string eventId, string eventType, string source, object payload, Func<Task<bool>> processor)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if webhook already exists (idempotency check)
                var existingWebhook = await _context.WebhookEvents
                    .FirstOrDefaultAsync(w => w.EventId == eventId);

                if (existingWebhook != null)
                {
                    if (existingWebhook.Status == "PROCESSED")
                    {
                        _logger.LogInformation("Webhook {EventId} already processed, ignoring duplicate", eventId);
                        return true;
                    }
                    else if (existingWebhook.Status == "PROCESSING")
                    {
                        _logger.LogWarning("Webhook {EventId} is currently being processed, ignoring duplicate", eventId);
                        return false;
                    }
                    else if (existingWebhook.Status == "FAILED" && existingWebhook.RetryCount >= 5)
                    {
                        _logger.LogError("Webhook {EventId} has exceeded maximum retry attempts", eventId);
                        return false;
                    }
                }

                // Create or update webhook event record
                var webhookEvent = existingWebhook ?? new WebhookEvent
                {
                    EventId = eventId,
                    EventType = eventType,
                    Source = source,
                    Payload = JsonSerializer.Serialize(payload),
                    ReceivedAt = DateTime.UtcNow
                };

                webhookEvent.Status = "PROCESSING";
                webhookEvent.RetryCount++;

                if (existingWebhook == null)
                {
                    _context.WebhookEvents.Add(webhookEvent);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Process the webhook
                try
                {
                    var success = await processor();
                    
                    if (success)
                    {
                        await MarkWebhookAsProcessedAsync(eventId, "Successfully processed");
                        _logger.LogInformation("Webhook {EventId} processed successfully", eventId);
                        return true;
                    }
                    else
                    {
                        await MarkWebhookAsFailedAsync(eventId, "Processor returned false");
                        _logger.LogWarning("Webhook {EventId} processing failed", eventId);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    await MarkWebhookAsFailedAsync(eventId, ex.Message);
                    _logger.LogError(ex, "Error processing webhook {EventId}", eventId);
                    return false;
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in webhook processing pipeline for {EventId}", eventId);
                return false;
            }
        }

        public async Task<WebhookEvent?> GetWebhookEventAsync(string eventId)
        {
            return await _context.WebhookEvents
                .FirstOrDefaultAsync(w => w.EventId == eventId);
        }

        public async Task<bool> IsWebhookProcessedAsync(string eventId)
        {
            return await _context.WebhookEvents
                .AnyAsync(w => w.EventId == eventId && w.Status == "PROCESSED");
        }

        public async Task MarkWebhookAsProcessedAsync(string eventId, string result)
        {
            var webhook = await _context.WebhookEvents
                .FirstOrDefaultAsync(w => w.EventId == eventId);

            if (webhook != null)
            {
                webhook.Status = "PROCESSED";
                webhook.ProcessingResult = result;
                webhook.ProcessedAt = DateTime.UtcNow;
                webhook.ErrorMessage = null;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkWebhookAsFailedAsync(string eventId, string errorMessage)
        {
            var webhook = await _context.WebhookEvents
                .FirstOrDefaultAsync(w => w.EventId == eventId);

            if (webhook != null)
            {
                webhook.Status = "FAILED";
                webhook.ErrorMessage = errorMessage;
                webhook.ProcessedAt = DateTime.UtcNow;
                
                // Calculate next retry time with exponential backoff
                var backoffMinutes = Math.Pow(2, webhook.RetryCount - 1); // 1, 2, 4, 8, 16 minutes
                webhook.NextRetryAt = DateTime.UtcNow.AddMinutes(backoffMinutes);
                
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<WebhookEvent>> GetFailedWebhooksForRetryAsync()
        {
            return await _context.WebhookEvents
                .Where(w => w.Status == "FAILED" 
                           && w.RetryCount < 5 
                           && w.NextRetryAt <= DateTime.UtcNow)
                .OrderBy(w => w.NextRetryAt)
                .Take(50) // Limit batch size
                .ToListAsync();
        }

        public async Task RetryFailedWebhookAsync(int webhookEventId)
        {
            var webhook = await _context.WebhookEvents.FindAsync(webhookEventId);
            if (webhook != null && webhook.Status == "FAILED" && webhook.RetryCount < 5)
            {
                webhook.Status = "PENDING";
                webhook.NextRetryAt = null;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Webhook {EventId} marked for retry (attempt {RetryCount})", 
                    webhook.EventId, webhook.RetryCount + 1);
            }
        }
    }
}