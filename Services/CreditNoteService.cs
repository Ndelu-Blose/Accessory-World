using AccessoryWorld.Models;
using AccessoryWorld.Models.Api;
using AccessoryWorld.Exceptions;
using AccessoryWorld.Data;
using Microsoft.EntityFrameworkCore;

namespace AccessoryWorld.Services
{
    public interface ICreditNoteService
    {
        Task<CreditNote> GetCreditNoteAsync(string creditNoteCode);
        Task<CreditNote> GetCreditNoteByIdAsync(int id);
        Task<IEnumerable<CreditNote>> GetUserCreditNotesAsync(string userId, bool activeOnly = true);
        Task<decimal> GetUserCreditBalanceAsync(string userId);
        Task<CreditNoteValidationResult> ValidateCreditNoteAsync(string creditNoteCode, decimal requestedAmount);
        Task<CreditNote?> ValidateCreditNoteAsync(string creditNoteCode, string userId);
        Task<decimal> ApplyCreditNoteToOrderAsync(string creditNoteCode, int orderId, decimal requestedAmount);
        Task<decimal> ApplyCreditNoteAsync(string creditNoteCode, decimal amount, int orderId);
        Task<bool> CancelCreditNoteAsync(int creditNoteId, string reason);
        Task<int> ExpireOldCreditNotesAsync();
        Task<CreditNoteUsageHistory> GetCreditNoteUsageHistoryAsync(string creditNoteCode);
        Task<IEnumerable<CreditNote>> GetExpiringCreditNotesAsync(int daysBeforeExpiry = 7);
    }

    public class CreditNoteService : ICreditNoteService
    {
        private readonly ApplicationDbContext _context;
        private readonly TradeInDomainService _domainService;
        private readonly ILogger<CreditNoteService> _logger;

        public CreditNoteService(
            ApplicationDbContext context,
            TradeInDomainService domainService,
            ILogger<CreditNoteService> logger)
        {
            _context = context;
            _domainService = domainService;
            _logger = logger;
        }

        public async Task<CreditNote> GetCreditNoteAsync(string creditNoteCode)
        {
            var creditNote = await _context.CreditNotes
                .Include(cn => cn.User)
                .Include(cn => cn.ConsumedInOrder)
                .FirstOrDefaultAsync(cn => cn.CreditNoteCode == creditNoteCode);

            if (creditNote == null)
                throw new DomainException($"Credit note {creditNoteCode} not found");

            return creditNote;
        }

        public async Task<CreditNote> GetCreditNoteByIdAsync(int id)
        {
            var creditNote = await _context.CreditNotes
                .Include(cn => cn.User)
                .Include(cn => cn.ConsumedInOrder)
                .FirstOrDefaultAsync(cn => cn.Id == id);

            if (creditNote == null)
                throw new DomainException($"Credit note with ID {id} not found");

            return creditNote;
        }

        public async Task<IEnumerable<CreditNote>> GetUserCreditNotesAsync(string userId, bool activeOnly = true)
        {
            var query = _context.CreditNotes
                .Where(cn => cn.UserId == userId);

            if (activeOnly)
            {
                query = query.Where(cn => cn.Status == TradeInDomainService.CreditNoteStatus.Active || 
                                        cn.Status == TradeInDomainService.CreditNoteStatus.PartiallyUsed);
            }

            return await query
                .OrderByDescending(cn => cn.CreatedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetUserCreditBalanceAsync(string userId)
        {
            return await _context.CreditNotes
                .Where(cn => cn.UserId == userId)
                .Where(cn => cn.Status == TradeInDomainService.CreditNoteStatus.Active || 
                           cn.Status == TradeInDomainService.CreditNoteStatus.PartiallyUsed)
                .Where(cn => cn.ExpiresAt > DateTime.UtcNow)
                .SumAsync(cn => cn.AmountRemaining);
        }

        public async Task<CreditNoteValidationResult> ValidateCreditNoteAsync(string creditNoteCode, decimal requestedAmount)
        {
            var result = new CreditNoteValidationResult
            {
                CreditNoteCode = creditNoteCode,
                RequestedAmount = requestedAmount
            };

            try
            {
                var creditNote = await GetCreditNoteAsync(creditNoteCode);
                
                result.CreditNote = creditNote;
                result.IsValid = true;

                // Check if credit note is active
                if (creditNote.Status == TradeInDomainService.CreditNoteStatus.Expired)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Credit note has expired";
                    return result;
                }

                if (creditNote.Status == TradeInDomainService.CreditNoteStatus.Cancelled)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Credit note has been cancelled";
                    return result;
                }

                if (creditNote.Status == TradeInDomainService.CreditNoteStatus.FullyUsed)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Credit note has been fully used";
                    return result;
                }

                // Check expiry date
                if (creditNote.ExpiresAt < DateTime.UtcNow)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Credit note has expired";
                    return result;
                }

                // Check requested amount
                if (requestedAmount <= 0)
                {
                    result.IsValid = false;
                    result.ErrorMessage = "Requested amount must be greater than zero";
                    return result;
                }

                if (requestedAmount > creditNote.AmountRemaining)
                {
                    result.IsValid = false;
                    result.ErrorMessage = $"Requested amount ({requestedAmount:C}) exceeds available balance ({creditNote.AmountRemaining:C})";
                    return result;
                }

                result.AvailableAmount = creditNote.AmountRemaining;
                result.ApplicableAmount = Math.Min(requestedAmount, creditNote.AmountRemaining);
            }
            catch (DomainException ex)
            {
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        public async Task<decimal> ApplyCreditNoteToOrderAsync(string creditNoteCode, int orderId, decimal requestedAmount)
        {
            // Validate first
            var validation = await ValidateCreditNoteAsync(creditNoteCode, requestedAmount);
            if (!validation.IsValid)
                throw new DomainException(validation.ErrorMessage ?? "Credit note validation failed");

            // Apply the credit note
            var appliedAmount = await _domainService.ApplyCreditNoteAsync(creditNoteCode, orderId, requestedAmount);

            _logger.LogInformation("Applied {AppliedAmount} from credit note {CreditNoteCode} to order {OrderId}", 
                appliedAmount, creditNoteCode, orderId);

            return appliedAmount;
        }

        public async Task<bool> CancelCreditNoteAsync(int creditNoteId, string reason)
        {
            var creditNote = await GetCreditNoteByIdAsync(creditNoteId);

            if (creditNote.Status == TradeInDomainService.CreditNoteStatus.FullyUsed)
                throw new DomainException("Cannot cancel a fully used credit note");

            if (creditNote.Status == TradeInDomainService.CreditNoteStatus.Cancelled)
                throw new DomainException("Credit note is already cancelled");

            creditNote.Status = TradeInDomainService.CreditNoteStatus.Cancelled;
            creditNote.AmountRemaining = 0;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Credit note {CreditNoteCode} cancelled. Reason: {Reason}", 
                creditNote.CreditNoteCode, reason);

            return true;
        }

        public async Task<int> ExpireOldCreditNotesAsync()
        {
            var expiredCreditNotes = await _context.CreditNotes
                .Where(cn => cn.ExpiresAt < DateTime.UtcNow)
                .Where(cn => cn.Status == TradeInDomainService.CreditNoteStatus.Active || 
                           cn.Status == TradeInDomainService.CreditNoteStatus.PartiallyUsed)
                .ToListAsync();

            foreach (var creditNote in expiredCreditNotes)
            {
                creditNote.Status = TradeInDomainService.CreditNoteStatus.Expired;
                _logger.LogInformation("Credit note {CreditNoteCode} expired with remaining balance {Amount}", 
                    creditNote.CreditNoteCode, creditNote.AmountRemaining);
            }

            if (expiredCreditNotes.Any())
            {
                await _context.SaveChangesAsync();
            }

            return expiredCreditNotes.Count;
        }

        public async Task<CreditNoteUsageHistory> GetCreditNoteUsageHistoryAsync(string creditNoteCode)
        {
            var creditNote = await GetCreditNoteAsync(creditNoteCode);
            
            var history = new CreditNoteUsageHistory
            {
                CreditNote = creditNote,
                UsageEvents = new List<CreditNoteUsageEvent>()
            };

            // Add creation event
            history.UsageEvents.Add(new CreditNoteUsageEvent
            {
                EventType = "CREATED",
                Amount = creditNote.Amount,
                Timestamp = creditNote.CreatedAt,
                Description = "Credit note created"
            });

            // Add redemption events if any
            if (creditNote.ConsumedInOrderId.HasValue && creditNote.RedeemedAt.HasValue)
            {
                var usedAmount = creditNote.Amount - creditNote.AmountRemaining;
                history.UsageEvents.Add(new CreditNoteUsageEvent
                {
                    EventType = "REDEEMED",
                    Amount = usedAmount,
                    Timestamp = creditNote.RedeemedAt.Value,
                    Description = $"Applied to order #{creditNote.ConsumedInOrderId}",
                    OrderId = creditNote.ConsumedInOrderId
                });
            }

            // Add status change events
            if (creditNote.Status == TradeInDomainService.CreditNoteStatus.Expired)
            {
                history.UsageEvents.Add(new CreditNoteUsageEvent
                {
                    EventType = "EXPIRED",
                    Amount = 0,
                    Timestamp = creditNote.ExpiresAt,
                    Description = "Credit note expired"
                });
            }

            history.UsageEvents = history.UsageEvents.OrderBy(e => e.Timestamp).ToList();
            return history;
        }

        public async Task<IEnumerable<CreditNote>> GetExpiringCreditNotesAsync(int daysBeforeExpiry = 7)
        {
            var expiryThreshold = DateTime.UtcNow.AddDays(daysBeforeExpiry);
            
            return await _context.CreditNotes
                .Include(cn => cn.User)
                .Where(cn => cn.ExpiresAt <= expiryThreshold && cn.ExpiresAt > DateTime.UtcNow)
                .Where(cn => cn.Status == TradeInDomainService.CreditNoteStatus.Active || 
                           cn.Status == TradeInDomainService.CreditNoteStatus.PartiallyUsed)
                .Where(cn => cn.AmountRemaining > 0)
                .OrderBy(cn => cn.ExpiresAt)
                .ToListAsync();
        }

        public async Task<CreditNote?> ValidateCreditNoteAsync(string creditNoteCode, string userId)
        {
            try
            {
                var creditNote = await _context.CreditNotes
                    .FirstOrDefaultAsync(cn => cn.CreditNoteCode == creditNoteCode && cn.UserId == userId);

                if (creditNote == null)
                    return null;

                // Check if credit note is active and not expired
                if (creditNote.Status != TradeInDomainService.CreditNoteStatus.Active &&
                    creditNote.Status != TradeInDomainService.CreditNoteStatus.PartiallyUsed)
                    return null;

                if (creditNote.ExpiresAt < DateTime.UtcNow)
                    return null;

                return creditNote;
            }
            catch
            {
                return null;
            }
        }

        public async Task<decimal> ApplyCreditNoteAsync(string creditNoteCode, decimal amount, int orderId)
        {
            var appliedAmount = await _domainService.ApplyCreditNoteAsync(creditNoteCode, orderId, amount);
            
            _logger.LogInformation("Applied {AppliedAmount} from credit note {CreditNoteCode} to order {OrderId}", 
                appliedAmount, creditNoteCode, orderId);

            return appliedAmount;
        }
    }

    // DTOs and Result Classes
    public class CreditNoteValidationResult
    {
        public string CreditNoteCode { get; set; } = string.Empty;
        public decimal RequestedAmount { get; set; }
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal AvailableAmount { get; set; }
        public decimal ApplicableAmount { get; set; }
        public CreditNote? CreditNote { get; set; }
    }

    public class CreditNoteUsageHistory
    {
        public CreditNote CreditNote { get; set; } = null!;
        public List<CreditNoteUsageEvent> UsageEvents { get; set; } = new();
    }

    public class CreditNoteUsageEvent
    {
        public string EventType { get; set; } = string.Empty; // CREATED, REDEEMED, EXPIRED, CANCELLED
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public int? OrderId { get; set; }
    }
}