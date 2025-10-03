namespace AccessoryWorld.Services.AI
{
    /// <summary>
    /// Interface for AI-powered device assessment providers
    /// </summary>
    public interface IDeviceAssessmentProvider
    {
        /// <summary>
        /// Analyzes device photos to determine condition and damage
        /// </summary>
        /// <param name="imageUrls">List of image URLs to analyze</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Device assessment result</returns>
        Task<DeviceAssessmentResult> AnalyzeAsync(
            List<string> imageUrls, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes device photos with additional context
        /// </summary>
        /// <param name="request">Assessment request with images and context</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Device assessment result</returns>
        Task<DeviceAssessmentResult> AnalyzeAsync(
            DeviceAssessmentRequest request, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the provider name (e.g., "trae-ai")
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Gets the model version being used
        /// </summary>
        string ModelVersion { get; }
    }
}