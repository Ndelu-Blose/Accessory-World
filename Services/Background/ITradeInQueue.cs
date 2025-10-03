namespace AccessoryWorld.Services.Background
{
    /// <summary>
    /// Interface for queuing trade-ins for background AI assessment
    /// </summary>
    public interface ITradeInQueue
    {
        /// <summary>
        /// Enqueues a trade-in for AI assessment processing
        /// </summary>
        /// <param name="tradeInId">Trade-in ID to process</param>
        /// <param name="priority">Processing priority (higher = more urgent)</param>
        /// <param name="delayMinutes">Optional delay in minutes before processing</param>
        /// <returns>Task representing the enqueue operation</returns>
        Task EnqueueAsync(int tradeInId, int priority = 0, int delayMinutes = 0);

        /// <summary>
        /// Dequeues the next trade-in for processing
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Trade-in ID to process, or null if queue is empty</returns>
        Task<int?> DequeueAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current queue length
        /// </summary>
        /// <returns>Number of items in queue</returns>
        Task<int> GetQueueLengthAsync();

        /// <summary>
        /// Checks if a trade-in is currently in the queue
        /// </summary>
        /// <param name="tradeInId">Trade-in ID to check</param>
        /// <returns>True if the trade-in is queued for processing</returns>
        Task<bool> IsQueuedAsync(int tradeInId);
    }
}