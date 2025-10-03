using AccessoryWorld.Models;
using AccessoryWorld.Exceptions;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;

namespace AccessoryWorld.Services
{
    public class TradeInDomainService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TradeInDomainService> _logger;

        public TradeInDomainService(ApplicationDbContext context, ILogger<TradeInDomainService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Trade-In Status State Machine
        public static class TradeInStatus
        {
            public const string Submitted = "SUBMITTED";
            public const string UnderReview = "UNDER_REVIEW";
            public const string Evaluated = "EVALUATED";
            public const string OfferSent = "OFFER_SENT";
            public const string Accepted = "ACCEPTED";
            public const string Rejected = "REJECTED";
            public const string Expired = "EXPIRED";
            public const string Completed = "COMPLETED";
            public const string Cancelled = "CANCELLED";
        }

        // Credit Note Status State Machine
        public static class CreditNoteStatus
        {
            public const string Active = "ACTIVE";
            public const string PartiallyUsed = "PARTIALLY_USED";
            public const string FullyUsed = "FULLY_USED";
            public const string Expired = "EXPIRED";
            public const string Cancelled = "CANCELLED";
        }

        // Condition Grades
        public static class ConditionGrade
        {
            public const string A = "A"; // Excellent
            public const string B = "B"; // Good
            public const string C = "C"; // Fair
            public const string D = "D"; // Poor
        }

        // Valid state transitions for Trade-In
        private static readonly Dictionary<string, List<string>> TradeInStateTransitions = new()
        {
            [TradeInStatus.Submitted] = new() { TradeInStatus.UnderReview, TradeInStatus.Cancelled },
            [TradeInStatus.UnderReview] = new() { TradeInStatus.Evaluated, TradeInStatus.Cancelled },
            [TradeInStatus.Evaluated] = new() { TradeInStatus.OfferSent, TradeInStatus.Cancelled },
            [TradeInStatus.OfferSent] = new() { TradeInStatus.Accepted, TradeInStatus.Rejected, TradeInStatus.Expired },
            [TradeInStatus.Accepted] = new() { TradeInStatus.Completed, TradeInStatus.Cancelled },
            [TradeInStatus.Rejected] = new() { }, // Terminal state
            [TradeInStatus.Expired] = new() { }, // Terminal state
            [TradeInStatus.Completed] = new() { }, // Terminal state
            [TradeInStatus.Cancelled] = new() { } // Terminal state
        };

        // Valid state transitions for Credit Note
        private static readonly Dictionary<string, List<string>> CreditNoteStateTransitions = new()
        {
            [CreditNoteStatus.Active] = new() { CreditNoteStatus.PartiallyUsed, CreditNoteStatus.FullyUsed, CreditNoteStatus.Expired, CreditNoteStatus.Cancelled },
            [CreditNoteStatus.PartiallyUsed] = new() { CreditNoteStatus.FullyUsed, CreditNoteStatus.Expired, CreditNoteStatus.Cancelled },
            [CreditNoteStatus.FullyUsed] = new() { }, // Terminal state
            [CreditNoteStatus.Expired] = new() { }, // Terminal state
            [CreditNoteStatus.Cancelled] = new() { } // Terminal state
        };

        /// <summary>
        /// Validates and transitions Trade-In to a new status
        /// </summary>
        public async Task<bool> TransitionTradeInStatusAsync(int tradeInId, string newStatus, string? approvedBy = null, string? notes = null)
        {
            var tradeIn = await _context.TradeIns.FindAsync(tradeInId);
            if (tradeIn == null)
                throw new DomainException($"Trade-In with ID {tradeInId} not found");

            // Validate state transition
            if (!IsValidTradeInTransition(tradeIn.Status, newStatus))
                throw new DomainException($"Invalid state transition from {tradeIn.Status} to {newStatus}");

            // Apply business rules for specific transitions
            ValidateTradeInTransitionRules(tradeIn, newStatus, approvedBy);

            // Update the trade-in
            tradeIn.Status = newStatus;
            if (newStatus == TradeInStatus.Evaluated || newStatus == TradeInStatus.OfferSent)
            {
                tradeIn.ReviewedAt = DateTime.UtcNow;
                tradeIn.ApprovedBy = approvedBy;
            }

            if (!string.IsNullOrEmpty(notes))
            {
                tradeIn.Notes = string.IsNullOrEmpty(tradeIn.Notes) ? notes : $"{tradeIn.Notes}\n{DateTime.UtcNow:yyyy-MM-dd HH:mm}: {notes}";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Trade-In {TradeInId} transitioned from {OldStatus} to {NewStatus}", 
                tradeInId, tradeIn.Status, newStatus);

            return true;
        }

        /// <summary>
        /// Creates a credit note from an accepted trade-in
        /// </summary>
        public async Task<CreditNote> CreateCreditNoteAsync(int tradeInId, decimal amount, int validityDays = 365)
        {
            var tradeIn = await _context.TradeIns.FindAsync(tradeInId);
            if (tradeIn == null)
                throw new DomainException($"Trade-In with ID {tradeInId} not found");

            if (tradeIn.Status != TradeInStatus.Accepted)
                throw new DomainException("Credit note can only be created for accepted trade-ins");

            if (amount <= 0)
                throw new DomainException("Credit note amount must be greater than zero");

            // Check if credit note already exists
            if (tradeIn.CreditNoteId.HasValue)
                throw new DomainException("Credit note already exists for this trade-in");

            var creditNote = new CreditNote
            {
                UserId = tradeIn.CustomerId,
                CreditNoteCode = GenerateCreditNoteCode(),
                Amount = amount,
                AmountRemaining = amount,
                Status = CreditNoteStatus.Active,
                ExpiresAt = DateTime.UtcNow.AddDays(validityDays),
                CreatedAt = DateTime.UtcNow
            };

            _context.CreditNotes.Add(creditNote);
            await _context.SaveChangesAsync();

            // Link the CreditNote to the TradeIn
            tradeIn.CreditNoteId = creditNote.Id;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Credit note {CreditNoteCode} created for Trade-In {TradeInId} with amount {Amount}", 
                creditNote.CreditNoteCode, tradeInId, amount);

            return creditNote;
        }

        /// <summary>
        /// Applies credit note to an order
        /// </summary>
        public async Task<decimal> ApplyCreditNoteAsync(string creditNoteCode, int orderId, decimal requestedAmount)
        {
            var creditNote = await _context.CreditNotes
                .FirstOrDefaultAsync(cn => cn.CreditNoteCode == creditNoteCode);

            if (creditNote == null)
                throw new DomainException($"Credit note {creditNoteCode} not found");

            ValidateCreditNoteUsage(creditNote, requestedAmount);

            var appliedAmount = Math.Min(requestedAmount, creditNote.AmountRemaining);
            creditNote.AmountRemaining -= appliedAmount;
            creditNote.ConsumedInOrderId = orderId;
            creditNote.RedeemedAt = DateTime.UtcNow;

            // Update status based on remaining amount
            if (creditNote.AmountRemaining == 0)
            {
                creditNote.Status = CreditNoteStatus.FullyUsed;
            }
            else if (creditNote.AmountRemaining < creditNote.Amount)
            {
                creditNote.Status = CreditNoteStatus.PartiallyUsed;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Applied {AppliedAmount} from credit note {CreditNoteCode} to order {OrderId}", 
                appliedAmount, creditNoteCode, orderId);

            return appliedAmount;
        }

        /// <summary>
        /// Validates Trade-In domain invariants
        /// </summary>
        public void ValidateTradeInInvariants(TradeIn tradeIn)
        {
            // Basic validation
            if (string.IsNullOrEmpty(tradeIn.CustomerId))
                throw new DomainException("Trade-In must have a customer");

            if (string.IsNullOrEmpty(tradeIn.DeviceBrand) || string.IsNullOrEmpty(tradeIn.DeviceModel))
                throw new DomainException("Trade-In must specify device brand and model");

            if (!IsValidConditionGrade(tradeIn.ConditionGrade))
                throw new DomainException($"Invalid condition grade: {tradeIn.ConditionGrade}");

            if (!IsValidTradeInStatus(tradeIn.Status))
                throw new DomainException($"Invalid trade-in status: {tradeIn.Status}");

            // Business rule: Approved value cannot exceed proposed value by more than 20%
            if (tradeIn.ApprovedValue.HasValue && tradeIn.ProposedValue.HasValue)
            {
                var maxApprovedValue = tradeIn.ProposedValue.Value * 1.2m;
                if (tradeIn.ApprovedValue.Value > maxApprovedValue)
                    throw new DomainException("Approved value cannot exceed proposed value by more than 20%");
            }

            // Business rule: Reviewed trade-ins must have an approver
            if ((tradeIn.Status == TradeInStatus.Evaluated || tradeIn.Status == TradeInStatus.OfferSent) 
                && string.IsNullOrEmpty(tradeIn.ApprovedBy))
                throw new DomainException("Reviewed trade-ins must have an approver");
        }

        /// <summary>
        /// Validates Credit Note domain invariants
        /// </summary>
        public void ValidateCreditNoteInvariants(CreditNote creditNote)
        {
            if (string.IsNullOrEmpty(creditNote.UserId))
                throw new DomainException("Credit note must have a user");

            if (creditNote.Amount <= 0)
                throw new DomainException("Credit note amount must be greater than zero");

            if (creditNote.AmountRemaining < 0)
                throw new DomainException("Credit note remaining amount cannot be negative");

            if (creditNote.AmountRemaining > creditNote.Amount)
                throw new DomainException("Credit note remaining amount cannot exceed original amount");

            if (!IsValidCreditNoteStatus(creditNote.Status))
                throw new DomainException($"Invalid credit note status: {creditNote.Status}");

            if (creditNote.ExpiresAt <= creditNote.CreatedAt)
                throw new DomainException("Credit note expiry date must be after creation date");

            // Business rule: Status must match remaining amount
            if (creditNote.AmountRemaining == 0 && creditNote.Status != CreditNoteStatus.FullyUsed)
                throw new DomainException("Credit note with zero remaining amount must have FULLY_USED status");

            if (creditNote.AmountRemaining == creditNote.Amount && 
                creditNote.Status != CreditNoteStatus.Active && 
                creditNote.Status != CreditNoteStatus.Expired && 
                creditNote.Status != CreditNoteStatus.Cancelled)
                throw new DomainException("Unused credit note must have ACTIVE, EXPIRED, or CANCELLED status");
        }

        // Private helper methods
        private bool IsValidTradeInTransition(string currentStatus, string newStatus)
        {
            return TradeInStateTransitions.ContainsKey(currentStatus) && 
                   TradeInStateTransitions[currentStatus].Contains(newStatus);
        }

        private void ValidateTradeInTransitionRules(TradeIn tradeIn, string newStatus, string? approvedBy)
        {
            switch (newStatus)
            {
                case TradeInStatus.Evaluated:
                case TradeInStatus.OfferSent:
                    if (string.IsNullOrEmpty(approvedBy))
                        throw new DomainException("Approver is required for evaluation transitions");
                    
                    if (!tradeIn.ApprovedValue.HasValue)
                        throw new DomainException("Approved value is required for evaluation transitions");
                    break;

                case TradeInStatus.Accepted:
                    if (!tradeIn.ApprovedValue.HasValue || tradeIn.ApprovedValue.Value <= 0)
                        throw new DomainException("Valid approved value is required to accept trade-in");
                    break;
            }
        }

        private void ValidateCreditNoteUsage(CreditNote creditNote, decimal requestedAmount)
        {
            if (creditNote.Status == CreditNoteStatus.Expired)
                throw new DomainException("Cannot use expired credit note");

            if (creditNote.Status == CreditNoteStatus.Cancelled)
                throw new DomainException("Cannot use cancelled credit note");

            if (creditNote.Status == CreditNoteStatus.FullyUsed)
                throw new DomainException("Credit note is fully used");

            if (creditNote.ExpiresAt < DateTime.UtcNow)
                throw new DomainException("Credit note has expired");

            if (requestedAmount <= 0)
                throw new DomainException("Requested amount must be greater than zero");

            if (requestedAmount > creditNote.AmountRemaining)
                throw new DomainException($"Requested amount ({requestedAmount:C}) exceeds remaining balance ({creditNote.AmountRemaining:C})");
        }

        private bool IsValidConditionGrade(string grade)
        {
            return grade == ConditionGrade.A || grade == ConditionGrade.B || 
                   grade == ConditionGrade.C || grade == ConditionGrade.D;
        }

        private bool IsValidTradeInStatus(string status)
        {
            return TradeInStateTransitions.ContainsKey(status);
        }

        private bool IsValidCreditNoteStatus(string status)
        {
            return CreditNoteStateTransitions.ContainsKey(status);
        }

        private string GenerateCreditNoteCode()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"CN{timestamp}{random}";
        }
    }
}