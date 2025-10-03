using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using AccessoryWorld.Models;
using AccessoryWorld.Services;
using AccessoryWorld.Exceptions;
using System.Security.Claims;

namespace AccessoryWorld.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TradeInApiController : ControllerBase
    {
        private readonly ITradeInService _tradeInService;
        private readonly ICreditNoteService _creditNoteService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TradeInApiController> _logger;

        public TradeInApiController(
            ITradeInService tradeInService,
            ICreditNoteService creditNoteService,
            UserManager<ApplicationUser> userManager,
            ILogger<TradeInApiController> logger)
        {
            _tradeInService = tradeInService;
            _creditNoteService = creditNoteService;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Create a new trade-in request
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TradeInResponse>> CreateTradeIn([FromBody] CreateTradeInApiRequest request)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var createRequest = new AccessoryWorld.Services.CreateTradeInRequest
                {
                    CustomerId = userId,
                    DeviceBrand = request.DeviceBrand,
                    DeviceModel = request.DeviceModel,
                    IMEI = request.IMEI,
                    ConditionGrade = request.ConditionGrade,
                    Photos = request.Photos,
                    ProposedValue = request.ProposedValue
                };

                var tradeIn = await _tradeInService.CreateTradeInAsync(createRequest);
                return Ok(new TradeInResponse(tradeIn));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trade-in");
                return StatusCode(500, new { error = "An error occurred while creating the trade-in" });
            }
        }

        /// <summary>
        /// Get trade-in by public ID
        /// </summary>
        [HttpGet("{publicId:guid}")]
        public async Task<ActionResult<TradeInResponse>> GetTradeIn(Guid publicId)
        {
            try
            {
                var tradeIn = await _tradeInService.GetTradeInByPublicIdAsync(publicId);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Users can only see their own trade-ins unless they're admin
                if (tradeIn.CustomerId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(new TradeInResponse(tradeIn));
            }
            catch (DomainException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trade-in {PublicId}", publicId);
                return StatusCode(500, new { error = "An error occurred while retrieving the trade-in" });
            }
        }

        /// <summary>
        /// Get user's trade-ins
        /// </summary>
        [HttpGet("my-trade-ins")]
        public async Task<ActionResult<IEnumerable<TradeInResponse>>> GetMyTradeIns()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var tradeIns = await _tradeInService.GetUserTradeInsAsync(userId);
                return Ok(tradeIns.Select(t => new TradeInResponse(t)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user trade-ins");
                return StatusCode(500, new { error = "An error occurred while retrieving trade-ins" });
            }
        }

        /// <summary>
        /// Accept a trade-in offer
        /// </summary>
        [HttpPost("{publicId:guid}/accept")]
        public async Task<ActionResult<CreditNoteResponse>> AcceptTradeIn(Guid publicId)
        {
            try
            {
                var tradeIn = await _tradeInService.GetTradeInByPublicIdAsync(publicId);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Users can only accept their own trade-ins
                if (tradeIn.CustomerId != userId)
                    return Forbid();

                var creditNote = await _tradeInService.AcceptTradeInAsync(tradeIn.Id);
                return Ok(new CreditNoteResponse(creditNote));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting trade-in {PublicId}", publicId);
                return StatusCode(500, new { error = "An error occurred while accepting the trade-in" });
            }
        }

        /// <summary>
        /// Reject a trade-in offer
        /// </summary>
        [HttpPost("{publicId:guid}/reject")]
        public async Task<ActionResult> RejectTradeIn(Guid publicId)
        {
            try
            {
                var tradeIn = await _tradeInService.GetTradeInByPublicIdAsync(publicId);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Users can only reject their own trade-ins
                if (tradeIn.CustomerId != userId)
                    return Forbid();

                await _tradeInService.UpdateTradeInStatusAsync(tradeIn.Id, TradeInDomainService.TradeInStatus.Rejected);
                return Ok(new { message = "Trade-in offer rejected" });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting trade-in {PublicId}", publicId);
                return StatusCode(500, new { error = "An error occurred while rejecting the trade-in" });
            }
        }

        // Admin endpoints
        /// <summary>
        /// Get all trade-ins (Admin only)
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public Task<ActionResult<PagedResult<TradeInResponse>>> GetAllTradeIns(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            try
            {
                // This would need to be implemented in the service
                // For now, return a simple response
                return Task.FromResult<ActionResult<PagedResult<TradeInResponse>>>(Ok(new { message = "Admin endpoint - implementation pending" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all trade-ins");
                return Task.FromResult<ActionResult<PagedResult<TradeInResponse>>>(StatusCode(500, new { error = "An error occurred while retrieving trade-ins" }));
            }
        }

        /// <summary>
        /// Evaluate a trade-in (Admin only)
        /// </summary>
        [HttpPost("admin/{publicId:guid}/evaluate")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TradeInResponse>> EvaluateTradeIn(Guid publicId, [FromBody] EvaluateTradeInRequest request)
        {
            try
            {
                var tradeIn = await _tradeInService.GetTradeInByPublicIdAsync(publicId);
                var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var updatedTradeIn = await _tradeInService.EvaluateTradeInAsync(
                    tradeIn.Id, 
                    request.ApprovedValue, 
                    adminUserId!, 
                    request.Notes);

                return Ok(new TradeInResponse(updatedTradeIn));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating trade-in {PublicId}", publicId);
                return StatusCode(500, new { error = "An error occurred while evaluating the trade-in" });
            }
        }

        /// <summary>
        /// Update trade-in status (Admin only)
        /// </summary>
        [HttpPut("admin/{publicId:guid}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TradeInResponse>> UpdateTradeInStatus(
            Guid publicId, 
            [FromBody] UpdateTradeInStatusRequest request)
        {
            try
            {
                var tradeIn = await _tradeInService.GetTradeInByPublicIdAsync(publicId);
                var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var updatedTradeIn = await _tradeInService.UpdateTradeInStatusAsync(
                    tradeIn.Id, 
                    request.Status, 
                    adminUserId, 
                    request.Notes);

                return Ok(new TradeInResponse(updatedTradeIn));
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trade-in status {PublicId}", publicId);
                return StatusCode(500, new { error = "An error occurred while updating the trade-in status" });
            }
        }

        /// <summary>
        /// Get trade-in statistics (Admin only)
        /// </summary>
        [HttpGet("admin/statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<TradeInStatistics>> GetStatistics()
        {
            try
            {
                var stats = await _tradeInService.GetTradeInStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trade-in statistics");
                return StatusCode(500, new { error = "An error occurred while retrieving statistics" });
            }
        }
    }

    // API Request/Response DTOs
    public class CreateTradeInApiRequest
    {
        public string DeviceBrand { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string? IMEI { get; set; }
        public string ConditionGrade { get; set; } = string.Empty;
        public List<string>? Photos { get; set; }
        public decimal? ProposedValue { get; set; }
    }

    public class EvaluateTradeInRequest
    {
        public decimal ApprovedValue { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateTradeInStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class TradeInResponse
    {
        public Guid PublicId { get; set; }
        public string DeviceBrand { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string? IMEI { get; set; }
        public string ConditionGrade { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal? ProposedValue { get; set; }
        public decimal? ApprovedValue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ApprovedByUserEmail { get; set; }
        public CreditNoteResponse? CreditNote { get; set; }

        public TradeInResponse(TradeIn tradeIn)
        {
            PublicId = tradeIn.PublicId;
            DeviceBrand = tradeIn.DeviceBrand;
            DeviceModel = tradeIn.DeviceModel;
            IMEI = tradeIn.IMEI;
            ConditionGrade = tradeIn.ConditionGrade;
            Status = tradeIn.Status;
            ProposedValue = tradeIn.ProposedValue;
            ApprovedValue = tradeIn.ApprovedValue;
            Notes = tradeIn.Notes;
            CreatedAt = tradeIn.CreatedAt.DateTime;
            ReviewedAt = tradeIn.ReviewedAt;
            ApprovedByUserEmail = tradeIn.ApprovedByUser?.Email;
            CreditNote = tradeIn.CreditNote != null ? new CreditNoteResponse(tradeIn.CreditNote) : null;
        }
    }

    public class CreditNoteResponse
    {
        public string CreditNoteCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AmountRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RedeemedAt { get; set; }
        public int? ConsumedInOrderId { get; set; }

        public CreditNoteResponse(CreditNote creditNote)
        {
            CreditNoteCode = creditNote.CreditNoteCode;
            Amount = creditNote.Amount;
            AmountRemaining = creditNote.AmountRemaining;
            Status = creditNote.Status;
            ExpiresAt = creditNote.ExpiresAt;
            CreatedAt = creditNote.CreatedAt;
            RedeemedAt = creditNote.RedeemedAt?.DateTime;
            ConsumedInOrderId = creditNote.ConsumedInOrderId;
        }
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}