using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AccessoryWorld.Models;
using AccessoryWorld.Services;
using AccessoryWorld.Models.Api;
using AccessoryWorld.Exceptions;
using System.Security.Claims;

namespace AccessoryWorld.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CreditNoteApiController : ControllerBase
    {
        private readonly ICreditNoteService _creditNoteService;
        private readonly ILogger<CreditNoteApiController> _logger;

        public CreditNoteApiController(
            ICreditNoteService creditNoteService,
            ILogger<CreditNoteApiController> logger)
        {
            _creditNoteService = creditNoteService;
            _logger = logger;
        }

        /// <summary>
        /// Get user's credit notes
        /// </summary>
        [HttpGet("my-credit-notes")]
        public async Task<ActionResult<IEnumerable<CreditNoteResponse>>> GetMyCreditNotes([FromQuery] bool activeOnly = true)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var creditNotes = await _creditNoteService.GetUserCreditNotesAsync(userId, activeOnly);
                return Ok(creditNotes.Select(cn => new CreditNoteResponse(cn)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user credit notes");
                return StatusCode(500, new { error = "An error occurred while retrieving credit notes" });
            }
        }

        /// <summary>
        /// Get user's total credit balance
        /// </summary>
        [HttpGet("my-balance")]
        public async Task<ActionResult<CreditBalanceResponse>> GetMyCreditBalance()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var balance = await _creditNoteService.GetUserCreditBalanceAsync(userId);
                return Ok(new CreditBalanceResponse { TotalBalance = balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user credit balance");
                return StatusCode(500, new { error = "An error occurred while retrieving credit balance" });
            }
        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateCreditNote([FromBody] ValidateCreditNoteRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var validation = await _creditNoteService.ValidateCreditNoteAsync(request.CreditNoteCode, request.RequestedAmount);
                
                var response = new ValidateCreditNoteResponse
                {
                    IsValid = validation.IsValid,
                    Code = validation.CreditNoteCode,
                    RemainingAmount = validation.AvailableAmount,
                    MaxApplicableAmount = validation.ApplicableAmount,
                    Message = validation.ErrorMessage ?? "Valid credit note"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credit note");
                return BadRequest(new ValidateCreditNoteResponse
                {
                    IsValid = false,
                    Message = "Error validating credit note"
                });
            }
        }

        [HttpPost("balance")]
        public async Task<IActionResult> GetCreditBalance([FromBody] ValidateCreditNoteRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var balance = await _creditNoteService.GetUserCreditBalanceAsync(userId);
                
                return Ok(new { Balance = balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting credit balance");
                return BadRequest(new { Error = "Error retrieving credit balance" });
            }
        }

        /// <summary>
        /// Get credit note details by code
        /// </summary>
        [HttpGet("{creditNoteCode}")]
        public async Task<ActionResult<CreditNoteResponse>> GetCreditNote(string creditNoteCode)
        {
            try
            {
                var creditNote = await _creditNoteService.GetCreditNoteAsync(creditNoteCode);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Users can only see their own credit notes unless they're admin
                if (creditNote.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                return Ok(new CreditNoteResponse(creditNote));
            }
            catch (DomainException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving credit note {CreditNoteCode}", creditNoteCode);
                return StatusCode(500, new { error = "An error occurred while retrieving the credit note" });
            }
        }

        /// <summary>
        /// Get credit note usage history
        /// </summary>
        [HttpGet("{creditNoteCode}/history")]
        public async Task<ActionResult<CreditNoteUsageHistoryResponse>> GetCreditNoteHistory(string creditNoteCode)
        {
            try
            {
                var creditNote = await _creditNoteService.GetCreditNoteAsync(creditNoteCode);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Users can only see their own credit note history unless they're admin
                if (creditNote.UserId != userId && !User.IsInRole("Admin"))
                    return Forbid();

                var history = await _creditNoteService.GetCreditNoteUsageHistoryAsync(creditNoteCode);
                
                return Ok(new CreditNoteUsageHistoryResponse
                {
                    CreditNote = new CreditNoteResponse(history.CreditNote),
                    UsageEvents = history.UsageEvents.Select(e => new CreditNoteUsageEventResponse
                    {
                        EventType = e.EventType,
                        Amount = e.Amount,
                        Timestamp = e.Timestamp,
                        Description = e.Description,
                        OrderId = e.OrderId
                    }).ToList()
                });
            }
            catch (DomainException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving credit note history {CreditNoteCode}", creditNoteCode);
                return StatusCode(500, new { error = "An error occurred while retrieving credit note history" });
            }
        }

        // Admin endpoints
        /// <summary>
        /// Cancel a credit note (Admin only)
        /// </summary>
        [HttpPost("admin/{creditNoteId:int}/cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> CancelCreditNote(int creditNoteId, [FromBody] CancelCreditNoteRequest request)
        {
            try
            {
                await _creditNoteService.CancelCreditNoteAsync(creditNoteId, request.Reason);
                return Ok(new { message = "Credit note cancelled successfully" });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling credit note {CreditNoteId}", creditNoteId);
                return StatusCode(500, new { error = "An error occurred while cancelling the credit note" });
            }
        }

        /// <summary>
        /// Get expiring credit notes (Admin only)
        /// </summary>
        [HttpGet("admin/expiring")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<CreditNoteResponse>>> GetExpiringCreditNotes([FromQuery] int daysBeforeExpiry = 7)
        {
            try
            {
                var expiringCreditNotes = await _creditNoteService.GetExpiringCreditNotesAsync(daysBeforeExpiry);
                return Ok(expiringCreditNotes.Select(cn => new CreditNoteResponse(cn)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving expiring credit notes");
                return StatusCode(500, new { error = "An error occurred while retrieving expiring credit notes" });
            }
        }

        /// <summary>
        /// Expire old credit notes (Admin only)
        /// </summary>
        [HttpPost("admin/expire-old")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ExpireCreditNotesResponse>> ExpireOldCreditNotes()
        {
            try
            {
                var expiredCount = await _creditNoteService.ExpireOldCreditNotesAsync();
                return Ok(new ExpireCreditNotesResponse { ExpiredCount = expiredCount });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring old credit notes");
                return StatusCode(500, new { error = "An error occurred while expiring credit notes" });
            }
        }

        /// <summary>
        /// Apply credit note to order (Internal use - called by checkout process)
        /// </summary>
        [HttpPost("apply-to-order")]
        [Authorize(Roles = "Admin,System")] // System role for internal API calls
        public async Task<ActionResult<ApplyCreditNoteResponse>> ApplyCreditNoteToOrder([FromBody] ApplyCreditNoteRequest request)
        {
            try
            {
                var appliedAmount = await _creditNoteService.ApplyCreditNoteToOrderAsync(
                    request.CreditNoteCode, 
                    request.OrderId, 
                    request.RequestedAmount);

                return Ok(new ApplyCreditNoteResponse { AppliedAmount = appliedAmount });
            }
            catch (DomainException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying credit note {CreditNoteCode} to order {OrderId}", 
                    request.CreditNoteCode, request.OrderId);
                return StatusCode(500, new { error = "An error occurred while applying the credit note" });
            }
        }
    }

    // API Request/Response DTOs
    public class ValidateCreditNoteRequest
    {
        public string CreditNoteCode { get; set; } = string.Empty;
        public decimal RequestedAmount { get; set; }
    }

    public class CancelCreditNoteRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class ApplyCreditNoteRequest
    {
        public string CreditNoteCode { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public decimal RequestedAmount { get; set; }
    }

    public class CreditBalanceResponse
    {
        public decimal TotalBalance { get; set; }
    }

    public class CreditNoteValidationResponse
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal AvailableAmount { get; set; }
        public decimal ApplicableAmount { get; set; }
        public CreditNoteResponse? CreditNote { get; set; }
    }

    public class CreditNoteUsageHistoryResponse
    {
        public CreditNoteResponse CreditNote { get; set; } = null!;
        public List<CreditNoteUsageEventResponse> UsageEvents { get; set; } = new();
    }

    public class CreditNoteUsageEventResponse
    {
        public string EventType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? OrderId { get; set; }
    }

    public class ExpireCreditNotesResponse
    {
        public int ExpiredCount { get; set; }
    }

    public class ApplyCreditNoteResponse
    {
        public decimal AppliedAmount { get; set; }
    }
}