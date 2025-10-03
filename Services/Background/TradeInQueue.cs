using System.Collections.Concurrent;

namespace AccessoryWorld.Services.Background
{
    /// <summary>
    /// In-memory implementation of trade-in queue for AI assessment processing
    /// </summary>
    public sealed class TradeInQueue : ITradeInQueue
    {
        private readonly ConcurrentQueue<QueueItem> _queue = new();
        private readonly ConcurrentDictionary<int, DateTime> _queuedItems = new();
        private readonly ILogger<TradeInQueue> _logger;

        public TradeInQueue(ILogger<TradeInQueue> logger)
        {
            _logger = logger;
        }

        public Task EnqueueAsync(int tradeInId, int priority = 0, int delayMinutes = 0)
        {
            // Check if already queued
            if (_queuedItems.ContainsKey(tradeInId))
            {
                _logger.LogInformation("TradeIn {TradeInId} is already queued for processing", tradeInId);
                return Task.CompletedTask;
            }

            var processAt = DateTime.UtcNow.AddMinutes(delayMinutes);
            var item = new QueueItem
            {
                TradeInId = tradeInId,
                Priority = priority,
                EnqueuedAt = DateTime.UtcNow,
                ProcessAt = processAt
            };

            _queue.Enqueue(item);
            _queuedItems.TryAdd(tradeInId, DateTime.UtcNow);

            _logger.LogInformation("Enqueued TradeIn {TradeInId} for AI assessment (Priority: {Priority}, Delay: {DelayMinutes}min, Queue Length: {QueueLength})", 
                tradeInId, priority, delayMinutes, _queue.Count);

            return Task.CompletedTask;
        }

        public Task<int?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            // Try to dequeue items, prioritizing by priority and then by enqueue time
            var items = new List<QueueItem>();
            
            // Collect all items from queue
            while (_queue.TryDequeue(out var item))
            {
                items.Add(item);
            }

            if (!items.Any())
            {
                return Task.FromResult<int?>(null);
            }

            // Filter items that are ready to be processed (ProcessAt time has passed)
            var readyItems = items.Where(i => DateTime.UtcNow >= i.ProcessAt).ToList();
            var notReadyItems = items.Where(i => DateTime.UtcNow < i.ProcessAt).ToList();

            // Put not-ready items back in the queue
            foreach (var notReadyItem in notReadyItems)
            {
                _queue.Enqueue(notReadyItem);
            }

            if (!readyItems.Any())
            {
                return Task.FromResult<int?>(null);
            }

            // Sort ready items by priority (descending) then by enqueue time (ascending)
            var sortedItems = readyItems
                .OrderByDescending(i => i.Priority)
                .ThenBy(i => i.EnqueuedAt)
                .ToList();

            // Take the highest priority item
            var nextItem = sortedItems.First();
            
            // Put the rest back in the queue
            foreach (var remainingItem in sortedItems.Skip(1))
            {
                _queue.Enqueue(remainingItem);
            }

            // Remove from queued items tracking
            _queuedItems.TryRemove(nextItem.TradeInId, out _);

            _logger.LogInformation("Dequeued TradeIn {TradeInId} for processing (Priority: {Priority}, Queued for: {Duration})", 
                nextItem.TradeInId, nextItem.Priority, DateTime.UtcNow - nextItem.EnqueuedAt);

            return Task.FromResult<int?>(nextItem.TradeInId);
        }

        public Task<int> GetQueueLengthAsync()
        {
            return Task.FromResult(_queue.Count);
        }

        public Task<bool> IsQueuedAsync(int tradeInId)
        {
            return Task.FromResult(_queuedItems.ContainsKey(tradeInId));
        }

        private sealed class QueueItem
        {
            public int TradeInId { get; set; }
            public int Priority { get; set; }
            public DateTime EnqueuedAt { get; set; }
            public DateTime ProcessAt { get; set; }
        }
    }
}