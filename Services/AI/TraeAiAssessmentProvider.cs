using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace AccessoryWorld.Services.AI
{
    /// <summary>
    /// Trae AI implementation of device assessment provider
    /// </summary>
    public sealed class TraeAiAssessmentProvider : IDeviceAssessmentProvider
    {
        private readonly HttpClient _httpClient;
        private readonly TraeAiOptions _options;
        private readonly ILogger<TraeAiAssessmentProvider> _logger;

        public TraeAiAssessmentProvider(
            HttpClient httpClient,
            IOptions<TraeAiOptions> options,
            ILogger<TraeAiAssessmentProvider> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public string ProviderName => "trae-ai";
        public string ModelVersion => _options.Model;

        public async Task<DeviceAssessmentResult> AnalyzeAsync(
            List<string> imageUrls, 
            CancellationToken cancellationToken = default)
        {
            var request = new DeviceAssessmentRequest
            {
                ImageUrls = imageUrls
            };

            return await AnalyzeAsync(request, cancellationToken);
        }

        public async Task<DeviceAssessmentResult> AnalyzeAsync(
            DeviceAssessmentRequest request, 
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("Starting Trae AI analysis for {ImageCount} images", request.ImageUrls.Count);

                // Prepare the request payload
                var requestPayload = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/analyze")
                {
                    Content = new StringContent(requestPayload, Encoding.UTF8, "application/json")
                };

                // Add authentication header
                httpRequest.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");

                httpRequest.Headers.Add("X-Model-Version", _options.Model);

                // Make the API call
                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Trae AI API returned {StatusCode}: {Content}", 
                        response.StatusCode, responseContent);
                    
                    // Categorize HTTP errors for better handling
                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "Invalid API key or authentication failed",
                        System.Net.HttpStatusCode.BadRequest => "Invalid request format or parameters",
                        System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded, please try again later",
                        System.Net.HttpStatusCode.InternalServerError => "Trae AI service is temporarily unavailable",
                        System.Net.HttpStatusCode.ServiceUnavailable => "Trae AI service is under maintenance",
                        _ => $"Trae AI API error: {response.StatusCode}"
                    };
                    
                    throw new HttpRequestException(errorMessage, null, response.StatusCode);
                }

                // Parse the response
                TraeAiResponse? traeResponse;
                try
                {
                    traeResponse = JsonSerializer.Deserialize<TraeAiResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                    });
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse Trae AI response: {ResponseContent}", responseContent);
                    throw new InvalidOperationException("Invalid response format from Trae AI service", ex);
                }

                if (traeResponse?.Success != true || traeResponse.Assessment == null)
                {
                    var error = traeResponse?.Error ?? "Unknown error";
                    _logger.LogError("Trae AI analysis failed: {Error}", error);
                    throw new InvalidOperationException($"Trae AI analysis failed: {error}");
                }

                // Set processing time and model version
                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                traeResponse.Assessment.ProcessingTimeMs = processingTime;
                traeResponse.Assessment.ModelVersion = _options.Model;

                _logger.LogInformation("Trae AI analysis completed in {ProcessingTime}ms with confidence {Confidence:P2}", 
                    processingTime, traeResponse.Assessment.IdentificationConfidence);

                return traeResponse.Assessment;
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError(ex, "Trae AI analysis timed out after {ProcessingTime}ms", processingTime);
                
                return CreateFailureResult(processingTime, "Analysis timed out");
            }
            catch (OperationCanceledException ex)
            {
                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning(ex, "Trae AI analysis was cancelled after {ProcessingTime}ms", processingTime);
                
                return CreateFailureResult(processingTime, "Analysis was cancelled");
            }
            catch (HttpRequestException ex)
            {
                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError(ex, "HTTP error during Trae AI analysis after {ProcessingTime}ms: {Message}", 
                    processingTime, ex.Message);
                
                return CreateFailureResult(processingTime, $"Network error: {ex.Message}");
            }
            catch (JsonException ex)
            {
                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError(ex, "JSON parsing error during Trae AI analysis after {ProcessingTime}ms", processingTime);
                
                return CreateFailureResult(processingTime, "Invalid response format");
            }
            catch (InvalidOperationException ex)
            {
                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError(ex, "Trae AI service error after {ProcessingTime}ms: {Message}", 
                    processingTime, ex.Message);
                
                return CreateFailureResult(processingTime, ex.Message);
            }
            catch (Exception ex)
            {
                var processingTime = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;
                _logger.LogError(ex, "Unexpected error during Trae AI analysis after {ProcessingTime}ms", processingTime);
                
                return CreateFailureResult(processingTime, "Unexpected error occurred");
            }
        }

        private DeviceAssessmentResult CreateFailureResult(int processingTime, string reason)
        {
            return new DeviceAssessmentResult
            {
                IdentificationConfidence = 0.0,
                ScreenCrackSeverity = 0.5, // Assume moderate damage when analysis fails
                BodyDentSeverity = 0.5,
                BackGlassSeverity = 0.5,
                CameraDamageSeverity = 0.5,
                WaterDamageLikelihood = 0.5,
                OverallConditionScore = 0.5,
                FunctionalIssues = new List<string> { $"Analysis failed: {reason} - manual review required" },
                ProcessingTimeMs = processingTime,
                ModelVersion = _options.Model
            };
        }
    }
}