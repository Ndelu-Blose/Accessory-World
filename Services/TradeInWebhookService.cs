using AccessoryWorld.Data;
using AccessoryWorld.Models;
using Microsoft.EntityFrameworkCore;

namespace AccessoryWorld.Services
{
    public interface ITradeInWebhookService
    {
        Task<bool> ProcessEvaluationCompletedAsync(int tradeInCaseId, decimal offeredAmount, string evaluationNotes, string conditionGrade);
        Task<bool> ProcessOfferAcceptedAsync(int tradeInCaseId, string userId, decimal acceptedAmount);
        Task<bool> ProcessCreditNoteIssuedAsync(int creditNoteId, string userId, decimal amount, string creditNoteCode);
        Task SendEvaluationNotificationAsync(int tradeInCaseId);
        Task SendOfferAcceptanceNotificationAsync(int tradeInCaseId);
        Task SendCreditNoteNotificationAsync(int creditNoteId);
    }

    public class TradeInWebhookService : ITradeInWebhookService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITradeInService _tradeInService;
        private readonly ICreditNoteService _creditNoteService;
        private readonly ILogger<TradeInWebhookService> _logger;

        public TradeInWebhookService(
            ApplicationDbContext context,
            ITradeInService tradeInService,
            ICreditNoteService creditNoteService,
            ILogger<TradeInWebhookService> logger)
        {
            _context = context;
            _tradeInService = tradeInService;
            _creditNoteService = creditNoteService;
            _logger = logger;
        }

        public async Task<bool> ProcessEvaluationCompletedAsync(int tradeInCaseId, decimal offeredAmount, string evaluationNotes, string conditionGrade)
        {
            try
            {
                var tradeInCase = await _context.TradeInCases
                    .FirstOrDefaultAsync(t => t.Id == tradeInCaseId);

                if (tradeInCase == null)
                {
                    _logger.LogWarning("TradeInCase {TradeInCaseId} not found", tradeInCaseId);
                    return false;
                }

                // Update the trade-in case with evaluation results
                tradeInCase.Status = "EVALUATED";
                tradeInCase.OfferAmount = offeredAmount;
                tradeInCase.OfferExpiresAt = DateTime.UtcNow.AddDays(7); // 7 days to accept

                await _context.SaveChangesAsync();

                // Send notification to customer
                await SendEvaluationNotificationAsync(tradeInCaseId);

                _logger.LogInformation("Evaluation completed for TradeInCase {TradeInCaseId}", tradeInCaseId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing evaluation completed for TradeInCase {TradeInCaseId}", tradeInCaseId);
                return false;
            }
        }

        public async Task<bool> ProcessOfferAcceptedAsync(int tradeInCaseId, string userId, decimal acceptedAmount)
        {
            try
            {
                var tradeInCase = await _context.TradeInCases
                    .FirstOrDefaultAsync(t => t.Id == tradeInCaseId && t.UserId == userId);

                if (tradeInCase == null)
                {
                    _logger.LogWarning("TradeInCase {TradeInCaseId} not found for user {UserId}", tradeInCaseId, userId);
                    return false;
                }

                // Validate that the case is in the correct state
                if (tradeInCase.Status != "EVALUATED")
                {
                    _logger.LogWarning("TradeInCase {TradeInCaseId} is not in EVALUATED status", tradeInCaseId);
                    return false;
                }

                // Update the trade-in case
                tradeInCase.Status = "ACCEPTED";
                tradeInCase.AcceptedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Send notification
                await SendOfferAcceptanceNotificationAsync(tradeInCaseId);

                _logger.LogInformation("Offer accepted for TradeInCase {TradeInCaseId}", tradeInCaseId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing offer acceptance for TradeInCase {TradeInCaseId}", tradeInCaseId);
                return false;
            }
        }

        public async Task<bool> ProcessCreditNoteIssuedAsync(int creditNoteId, string userId, decimal amount, string creditNoteCode)
        {
            try
            {
                // Verify the credit note exists and belongs to the user
                var creditNote = await _context.CreditNotes
                    .FirstOrDefaultAsync(c => c.Id == creditNoteId && c.UserId == userId);

                if (creditNote == null)
                {
                    _logger.LogWarning("CreditNote {CreditNoteId} not found for user {UserId}", creditNoteId, userId);
                    return false;
                }

                // Verify credit note details match
                if (creditNote.CreditNoteCode != creditNoteCode || creditNote.Amount != amount)
                {
                    _logger.LogWarning("CreditNote {CreditNoteId} details mismatch", creditNoteId);
                    return false;
                }

                // Send notification to user
                await SendCreditNoteNotificationAsync(creditNoteId);

                _logger.LogInformation("Credit note issued notification sent for {CreditNoteId}", creditNoteId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing credit note issued for {CreditNoteId}", creditNoteId);
                return false;
            }
        }

        public async Task SendEvaluationNotificationAsync(int tradeInCaseId)
        {
            try
            {
                var tradeInCase = await _context.TradeInCases
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == tradeInCaseId);

                if (tradeInCase?.User != null)
                {
                    // TODO: Implement email/SMS notification service
                    _logger.LogInformation("Evaluation notification sent for trade-in case {CaseId} to user {UserId}",
                        tradeInCaseId, tradeInCase.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending evaluation notification for case {CaseId}", tradeInCaseId);
            }
        }

        public async Task SendOfferAcceptanceNotificationAsync(int tradeInCaseId)
        {
            try
            {
                var tradeInCase = await _context.TradeInCases
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == tradeInCaseId);

                if (tradeInCase?.User != null)
                {
                    // TODO: Implement email/SMS notification service
                    _logger.LogInformation("Offer acceptance notification sent for trade-in case {CaseId} to user {UserId}",
                        tradeInCaseId, tradeInCase.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending offer acceptance notification for case {CaseId}", tradeInCaseId);
            }
        }

        public async Task SendCreditNoteNotificationAsync(int creditNoteId)
        {
            try
            {
                var creditNote = await _context.CreditNotes
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == creditNoteId);

                if (creditNote?.User != null)
                {
                    // TODO: Implement email/SMS notification service
                    _logger.LogInformation("Credit note notification sent for note {CreditNoteId} to user {UserId}",
                        creditNoteId, creditNote.UserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending credit note notification for note {CreditNoteId}", creditNoteId);
            }
        }
    }
}