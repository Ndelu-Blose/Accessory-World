using AccessoryWorld.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Net;

namespace AccessoryWorld.Services
{
    public class PerformanceValidationOptions
    {
        public int MaxRequestsPerMinute { get; set; } = 60;
        public int MaxRequestsPerHour { get; set; } = 1000;
        public int MaxConcurrentRequests { get; set; } = 10;
        public int QueryTimeoutSeconds { get; set; } = 30;
        public int MaxPageSize { get; set; } = 100;
        public int MaxSearchResults { get; set; } = 1000;
        public long MaxUploadSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
        public int MaxBulkOperationSize { get; set; } = 100;
        public bool EnableRateLimiting { get; set; } = true;
        public bool EnableQueryOptimization { get; set; } = true;
    }

    public interface IPerformanceValidationService
    {
        void ValidateRateLimit(string clientId, string endpoint);
        void ValidateConcurrencyLimit(string clientId);
        void ValidateQueryConstraints(int pageSize, int page, string? sortBy = null);
        void ValidateSearchConstraints(string query, int maxResults);
        void ValidateUploadSize(long fileSize, string fileName);
        void ValidateBulkOperationSize(int operationCount, string operationType);
        Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, int? timeoutSeconds = null);
        Task ExecuteWithTimeoutAsync(Func<Task> operation, int? timeoutSeconds = null);
        void RecordRequestStart(string requestId, string clientId);
        void RecordRequestEnd(string requestId);
        Task<bool> IsClientThrottledAsync(string clientId);
        void ResetClientLimits(string clientId);
        Dictionary<string, object> GetPerformanceMetrics();
    }

    public class PerformanceValidationService : IPerformanceValidationService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<PerformanceValidationService> _logger;
        private readonly PerformanceValidationOptions _options;
        private readonly ConcurrentDictionary<string, int> _concurrentRequests = new();
        private readonly ConcurrentDictionary<string, DateTime> _activeRequests = new();
        private readonly ConcurrentDictionary<string, List<DateTime>> _requestHistory = new();

        public PerformanceValidationService(
            IMemoryCache cache,
            ILogger<PerformanceValidationService> logger,
            IOptions<PerformanceValidationOptions> options)
        {
            _cache = cache;
            _logger = logger;
            _options = options.Value;
        }

        public void ValidateRateLimit(string clientId, string endpoint)
        {
            if (!_options.EnableRateLimiting)
                return;

            var now = DateTime.UtcNow;
            var minuteKey = $"rate_limit_minute_{clientId}_{endpoint}";
            var hourKey = $"rate_limit_hour_{clientId}_{endpoint}";

            // Check minute limit
            var minuteRequests = GetRequestCount(minuteKey, TimeSpan.FromMinutes(1));
            if (minuteRequests >= _options.MaxRequestsPerMinute)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}: {Count} requests per minute",
                    clientId, endpoint, minuteRequests);
                throw new DomainException(
                    "Rate limit exceeded. Too many requests per minute.",
                    DomainErrors.SYSTEM_ERROR);
            }

            // Check hour limit
            var hourRequests = GetRequestCount(hourKey, TimeSpan.FromHours(1));
            if (hourRequests >= _options.MaxRequestsPerHour)
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId} on endpoint {Endpoint}: {Count} requests per hour",
                    clientId, endpoint, hourRequests);
                throw new DomainException(
                    "Rate limit exceeded. Too many requests per hour.",
                    DomainErrors.SYSTEM_ERROR);
            }

            // Record this request
            IncrementRequestCount(minuteKey, TimeSpan.FromMinutes(1));
            IncrementRequestCount(hourKey, TimeSpan.FromHours(1));
        }

        public void ValidateConcurrencyLimit(string clientId)
        {
            var currentConcurrent = _concurrentRequests.GetOrAdd(clientId, 0);
            
            if (currentConcurrent >= _options.MaxConcurrentRequests)
            {
                _logger.LogWarning("Concurrency limit exceeded for client {ClientId}: {Count} concurrent requests",
                    clientId, currentConcurrent);
                throw new DomainException(
                    "Too many concurrent requests. Please wait and try again.",
                    DomainErrors.SYSTEM_ERROR);
            }

            _concurrentRequests.AddOrUpdate(clientId, 1, (key, value) => value + 1);
        }

        public void ValidateQueryConstraints(int pageSize, int page, string? sortBy = null)
        {
            if (pageSize <= 0)
            {
                throw new DomainException("Page size must be greater than 0", DomainErrors.INVALID_ORDER_STATE);
            }

            if (pageSize > _options.MaxPageSize)
            {
                throw new DomainException(
                    $"Page size cannot exceed {_options.MaxPageSize} items",
                    DomainErrors.INVALID_ORDER_STATE);
            }

            if (page < 1)
            {
                throw new DomainException("Page number must be greater than 0", DomainErrors.INVALID_ORDER_STATE);
            }

            // Prevent deep pagination which can be expensive
            var maxPage = _options.MaxSearchResults / pageSize;
            if (page > maxPage)
            {
                throw new DomainException(
                    $"Page number too high. Maximum page is {maxPage} for this page size",
                    DomainErrors.INVALID_ORDER_STATE);
            }

            // Validate sort field if provided
            if (!string.IsNullOrEmpty(sortBy))
            {
                var allowedSortFields = new[] { "id", "name", "price", "createdAt", "updatedAt", "status" };
                var sortField = sortBy.TrimStart('-').ToLowerInvariant();
                
                if (!allowedSortFields.Contains(sortField))
                {
                    throw new DomainException(
                        $"Invalid sort field '{sortBy}'. Allowed fields: {string.Join(", ", allowedSortFields)}",
                        DomainErrors.INVALID_ORDER_STATE);
                }
            }
        }

        public void ValidateSearchConstraints(string query, int maxResults)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new DomainException("Search query cannot be empty", DomainErrors.INVALID_ORDER_STATE);
            }

            if (query.Length < 2)
            {
                throw new DomainException("Search query must be at least 2 characters long", DomainErrors.INVALID_ORDER_STATE);
            }

            if (query.Length > 100)
            {
                throw new DomainException("Search query cannot exceed 100 characters", DomainErrors.INVALID_ORDER_STATE);
            }

            if (maxResults > _options.MaxSearchResults)
            {
                throw new DomainException(
                    $"Maximum search results cannot exceed {_options.MaxSearchResults}",
                    DomainErrors.INVALID_ORDER_STATE);
            }

            // Prevent potentially expensive wildcard searches
            if (query.StartsWith("*") || query.StartsWith("%"))
            {
                throw new DomainException(
                    "Wildcard searches at the beginning of query are not allowed",
                    DomainErrors.INVALID_ORDER_STATE);
            }
        }

        public void ValidateUploadSize(long fileSize, string fileName)
        {
            if (fileSize <= 0)
            {
                throw new DomainException("File size must be greater than 0", DomainErrors.INVALID_ORDER_STATE);
            }

            if (fileSize > _options.MaxUploadSizeBytes)
            {
                var maxSizeMB = _options.MaxUploadSizeBytes / (1024 * 1024);
                throw new DomainException(
                    $"File size cannot exceed {maxSizeMB}MB. Current file: {fileSize / (1024 * 1024):F2}MB",
                    DomainErrors.INVALID_ORDER_STATE);
            }

            // Validate file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx" };
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(extension))
            {
                throw new DomainException(
                    $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}",
                    DomainErrors.INVALID_ORDER_STATE);
            }
        }

        public void ValidateBulkOperationSize(int operationCount, string operationType)
        {
            if (operationCount <= 0)
            {
                throw new DomainException("Operation count must be greater than 0", DomainErrors.INVALID_ORDER_STATE);
            }

            if (operationCount > _options.MaxBulkOperationSize)
            {
                throw new DomainException(
                    $"Bulk {operationType} cannot exceed {_options.MaxBulkOperationSize} items. Current: {operationCount}",
                    DomainErrors.INVALID_ORDER_STATE);
            }
        }

        public async Task<T> ExecuteWithTimeoutAsync<T>(Func<Task<T>> operation, int? timeoutSeconds = null)
        {
            var timeout = timeoutSeconds ?? _options.QueryTimeoutSeconds;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout));
            
            try
            {
                var task = operation();
                var completedTask = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
                
                if (completedTask == task)
                {
                    return await task;
                }
                else
                {
                    _logger.LogWarning("Operation timed out after {Timeout} seconds", timeout);
                    throw new DomainException(
                        $"Operation timed out after {timeout} seconds",
                        DomainErrors.SYSTEM_ERROR);
                }
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                _logger.LogWarning("Operation was cancelled due to timeout after {Timeout} seconds", timeout);
                throw new DomainException(
                    $"Operation timed out after {timeout} seconds",
                    DomainErrors.SYSTEM_ERROR);
            }
        }

        public async Task ExecuteWithTimeoutAsync(Func<Task> operation, int? timeoutSeconds = null)
        {
            await ExecuteWithTimeoutAsync(async () =>
            {
                await operation();
                return true;
            }, timeoutSeconds);
        }

        public void RecordRequestStart(string requestId, string clientId)
        {
            _activeRequests[requestId] = DateTime.UtcNow;
            
            // Track request history for analytics
            _requestHistory.AddOrUpdate(clientId, 
                new List<DateTime> { DateTime.UtcNow },
                (key, existing) =>
                {
                    existing.Add(DateTime.UtcNow);
                    // Keep only last hour of requests
                    var cutoff = DateTime.UtcNow.AddHours(-1);
                    return existing.Where(dt => dt > cutoff).ToList();
                });
        }

        public void RecordRequestEnd(string requestId)
        {
            if (_activeRequests.TryRemove(requestId, out var startTime))
            {
                var duration = DateTime.UtcNow - startTime;
                _logger.LogDebug("Request {RequestId} completed in {Duration}ms", requestId, duration.TotalMilliseconds);
                
                // Log slow requests
                if (duration.TotalSeconds > 5)
                {
                    _logger.LogWarning("Slow request detected: {RequestId} took {Duration}ms", 
                        requestId, duration.TotalMilliseconds);
                }
            }

            // Decrement concurrent request count for the client
            // Note: We'd need to track clientId per request for this to work properly
            // This is a simplified implementation
        }

        public Task<bool> IsClientThrottledAsync(string clientId)
        {
            if (string.IsNullOrEmpty(clientId))
                return Task.FromResult(false);
            
            var key = $"throttle_{clientId}";
            return Task.FromResult(_cache.TryGetValue(key, out _));
        }

        public void ResetClientLimits(string clientId)
        {
            var minuteKey = $"rate_limit_minute_{clientId}";
            var hourKey = $"rate_limit_hour_{clientId}";
            
            _cache.Remove(minuteKey);
            _cache.Remove(hourKey);
            _concurrentRequests.TryRemove(clientId, out _);
            _requestHistory.TryRemove(clientId, out _);
            
            _logger.LogInformation("Reset rate limits for client {ClientId}", clientId);
        }

        public Dictionary<string, object> GetPerformanceMetrics()
        {
            var now = DateTime.UtcNow;
            var activeRequestsCount = _activeRequests.Count;
            var totalConcurrentRequests = _concurrentRequests.Values.Sum();
            
            // Calculate average request duration for active requests
            var activeRequestDurations = _activeRequests.Values
                .Select(startTime => (now - startTime).TotalMilliseconds)
                .ToList();
            
            var avgRequestDuration = activeRequestDurations.Any() 
                ? activeRequestDurations.Average() 
                : 0;
            
            var maxRequestDuration = activeRequestDurations.Any() 
                ? activeRequestDurations.Max() 
                : 0;

            return new Dictionary<string, object>
            {
                ["ActiveRequests"] = activeRequestsCount,
                ["TotalConcurrentRequests"] = totalConcurrentRequests,
                ["AverageRequestDurationMs"] = Math.Round(avgRequestDuration, 2),
                ["MaxRequestDurationMs"] = Math.Round(maxRequestDuration, 2),
                ["TotalClientsTracked"] = _requestHistory.Count,
                ["RateLimitingEnabled"] = _options.EnableRateLimiting,
                ["MaxRequestsPerMinute"] = _options.MaxRequestsPerMinute,
                ["MaxRequestsPerHour"] = _options.MaxRequestsPerHour,
                ["MaxConcurrentRequests"] = _options.MaxConcurrentRequests,
                ["QueryTimeoutSeconds"] = _options.QueryTimeoutSeconds
            };
        }

        private int GetRequestCount(string key, TimeSpan window)
        {
            if (_cache.TryGetValue(key, out int count))
            {
                return count;
            }
            return 0;
        }

        private void IncrementRequestCount(string key, TimeSpan window)
        {
            var count = GetRequestCount(key, window);
            _cache.Set(key, count + 1, window);
        }
    }


}