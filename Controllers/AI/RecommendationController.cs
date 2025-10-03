using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using AccessoryWorld.DTOs.AI;
using AccessoryWorld.Services.AI;
using System.Security.Claims;

namespace AccessoryWorld.Controllers.AI
{
    [ApiController]
    [Route("api/ai/recommendations")]
    public class RecommendationController : ControllerBase
    {
        private readonly IAIRecommendationService _recommendationService;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(
            IAIRecommendationService recommendationService,
            ILogger<RecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _logger = logger;
        }

        /// <summary>
        /// Get personalized product recommendations for a user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<RecommendationResponse>> GetRecommendations(
            [FromQuery] RecommendationRequest request)
        {
            try
            {
                // Get user ID from claims if not provided
                if (string.IsNullOrEmpty(request.UserId) && User.Identity?.IsAuthenticated == true)
                {
                    request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }

                var response = await _recommendationService.GetRecommendationsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations");
                return StatusCode(500, "An error occurred while getting recommendations");
            }
        }

        /// <summary>
        /// Get similar products for a given product
        /// </summary>
        [HttpGet("similar")]
        public async Task<ActionResult<SimilarProductResponse>> GetSimilarProducts(
            [FromQuery] SimilarProductRequest request)
        {
            try
            {
                var response = await _recommendationService.GetSimilarProductsAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar products for product {ProductId}", request.ProductId);
                return StatusCode(500, "An error occurred while getting similar products");
            }
        }

        /// <summary>
        /// Track user behavior for recommendation learning
        /// </summary>
        [HttpPost("behavior")]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> TrackBehavior([FromBody] UserBehaviorRequest request)
        {
            try
            {
                // Get user ID from claims if not provided
                if (string.IsNullOrEmpty(request.UserId) && User.Identity?.IsAuthenticated == true)
                {
                    request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                }

                if (string.IsNullOrEmpty(request.UserId))
                {
                    return BadRequest("User ID is required");
                }

                await _recommendationService.TrackUserBehaviorAsync(request);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking user behavior");
                return StatusCode(500, "An error occurred while tracking behavior");
            }
        }

        /// <summary>
        /// Record feedback on recommendations
        /// </summary>
        [HttpPost("feedback")]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RecordFeedback([FromBody] RecommendationFeedbackRequest request)
        {
            try
            {
                // Get user ID from claims
                request.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

                if (string.IsNullOrEmpty(request.UserId))
                {
                    return Unauthorized();
                }

                await _recommendationService.RecordRecommendationFeedbackAsync(request);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording recommendation feedback");
                return StatusCode(500, "An error occurred while recording feedback");
            }
        }

        /// <summary>
        /// Get user profile for recommendations
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserProfileResponse>> GetUserProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var profile = await _recommendationService.GetUserProfileAsync(userId);
                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, "An error occurred while getting user profile");
            }
        }

        /// <summary>
        /// Update user profile for recommendations
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateUserProfile([FromBody] UserProfileRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                request.UserId = userId;
                await _recommendationService.UpdateUserProfileAsync(request);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, "An error occurred while updating user profile");
            }
        }

        /// <summary>
        /// Get recommendation metrics for analytics
        /// </summary>
        [HttpGet("metrics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<RecommendationMetrics>>> GetMetrics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var metrics = await _recommendationService.GetRecommendationMetricsAsync(from, to);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendation metrics");
                return StatusCode(500, "An error occurred while getting metrics");
            }
        }

        /// <summary>
        /// Assign user to A/B test group
        /// </summary>
        [HttpPost("ab-test")]
        [Authorize]
        [IgnoreAntiforgeryToken]
        public async Task<ActionResult<ABTestResponse>> AssignToTestGroup([FromBody] ABTestRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var response = await _recommendationService.AssignUserToTestGroupAsync(userId, request.TestName);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning user to A/B test group");
                return StatusCode(500, "An error occurred while assigning to test group");
            }
        }
    }
}
