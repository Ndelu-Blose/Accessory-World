using AccessoryWorld.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using AccessoryWorld.Models.Api;

namespace AccessoryWorld.Controllers.Api
{
    [ApiController]
    [Route("api/webhooks/tradein")]
    public class TradeInWebhookController : ControllerBase
    {
        private readonly IWebhookService _webhookService;
        private readonly ITradeInService _tradeInService;
        private readonly ITradeInWebhookService _tradeInWebhookService;
        private readonly ICreditNoteService _creditNoteService;
        private readonly ILogger<TradeInWebhookController> _logger;

        public TradeInWebhookController(
            IWebhookService webhookService,
            ITradeInService tradeInService,
            ITradeInWebhookService tradeInWebhookService,
            ICreditNoteService creditNoteService,
            ILogger<TradeInWebhookController> logger)
        {
            _webhookService = webhookService;
            _tradeInService = tradeInService;
            _tradeInWebhookService = tradeInWebhookService;
            _creditNoteService = creditNoteService;
            _logger = logger;
        }

        [HttpPost("evaluation-completed")]
        public async Task<IActionResult> EvaluationCompleted([FromBody] TradeInEvaluationWebhook webhook)
        {
            try
            {
                var eventId = $"evaluation_completed_{webhook.TradeInCaseId}_{webhook.Timestamp:yyyyMMddHHmmss}";
                
                var success = await _webhookService.ProcessWebhookAsync(
                    eventId,
                    "EVALUATION_COMPLETED",
                    "TRADEIN_SYSTEM",
                    webhook,
                    async () =>
                    {
                        // Process the evaluation completion using TradeInWebhookService
                        return await _tradeInWebhookService.ProcessEvaluationCompletedAsync(
                            webhook.TradeInCaseId,
                            webhook.OfferedAmount,
                            webhook.EvaluationNotes,
                            webhook.ConditionGrade);
                    });

                if (success)
                {
                    return Ok(new { message = "Evaluation completed webhook processed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to process evaluation completed webhook" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing evaluation completed webhook for case {CaseId}", webhook.TradeInCaseId);
                return StatusCode(500, new { message = "Internal server error processing webhook" });
            }
        }

        [HttpPost("offer-accepted")]
        public async Task<IActionResult> OfferAccepted([FromBody] TradeInOfferWebhook webhook)
        {
            try
            {
                var eventId = $"offer_accepted_{webhook.TradeInCaseId}_{webhook.Timestamp:yyyyMMddHHmmss}";
                
                var success = await _webhookService.ProcessWebhookAsync(
                    eventId,
                    "OFFER_ACCEPTED",
                    "TRADEIN_SYSTEM",
                    webhook,
                    async () =>
                    {
                        // Process the offer acceptance using TradeInWebhookService
                        return await _tradeInWebhookService.ProcessOfferAcceptedAsync(
                            webhook.TradeInCaseId,
                            webhook.UserId,
                            webhook.AcceptedAmount);
                    });

                if (success)
                {
                    return Ok(new { message = "Offer acceptance webhook processed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to process offer acceptance webhook" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing offer acceptance webhook for case {CaseId}", webhook.TradeInCaseId);
                return StatusCode(500, new { message = "Internal server error processing webhook" });
            }
        }

        [HttpPost("credit-note-issued")]
        public async Task<IActionResult> CreditNoteIssued([FromBody] CreditNoteWebhook webhook)
        {
            try
            {
                var eventId = $"credit_note_{webhook.CreditNoteId}_{webhook.Timestamp:yyyyMMddHHmmss}";
                
                var success = await _webhookService.ProcessWebhookAsync(
                    eventId,
                    "CREDIT_NOTE_ISSUED",
                    "TRADEIN_SYSTEM",
                    webhook,
                    async () =>
                    {
                        // Verify the credit note was created correctly
                        var creditNote = await _creditNoteService.GetCreditNoteByIdAsync(webhook.CreditNoteId);
                        if (creditNote == null)
                        {
                            _logger.LogWarning("Credit note {CreditNoteId} not found for issuance webhook", webhook.CreditNoteId);
                            return false;
                        }

                        // Send notification to user about credit note availability
                        // This could trigger email/SMS notifications
                        _logger.LogInformation("Credit note {CreditNoteId} issued successfully for user {UserId}, amount: {Amount}",
                            webhook.CreditNoteId, webhook.UserId, webhook.Amount);

                        return true;
                    });

                if (success)
                {
                    return Ok(new { message = "Credit note issuance webhook processed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to process credit note issuance webhook" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing credit note issuance webhook for note {CreditNoteId}", webhook.CreditNoteId);
                return StatusCode(500, new { message = "Internal server error processing webhook" });
            }
        }

        [HttpGet("status/{eventId}")]
        public async Task<IActionResult> GetWebhookStatus(string eventId)
        {
            try
            {
                var webhookEvent = await _webhookService.GetWebhookEventAsync(eventId);
                if (webhookEvent == null)
                {
                    return NotFound(new { message = "Webhook event not found" });
                }

                return Ok(new
                {
                    eventId = webhookEvent.EventId,
                    eventType = webhookEvent.EventType,
                    status = webhookEvent.Status,
                    receivedAt = webhookEvent.ReceivedAt,
                    processedAt = webhookEvent.ProcessedAt,
                    retryCount = webhookEvent.RetryCount,
                    errorMessage = webhookEvent.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving webhook status for {EventId}", eventId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }

    // Webhook payload models
    public class TradeInEvaluationWebhook
    {
        public int TradeInCaseId { get; set; }
        public decimal OfferedAmount { get; set; }
        public string EvaluationNotes { get; set; } = string.Empty;
        public string ConditionGrade { get; set; } = string.Empty;
        public string EvaluatedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class TradeInOfferWebhook
    {
        public int TradeInCaseId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal AcceptedAmount { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CreditNoteWebhook
    {
        public int CreditNoteId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CreditNoteCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}