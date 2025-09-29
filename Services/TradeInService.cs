using AccessoryWorld.Models;
using AccessoryWorld.Exceptions;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using System.Text.Json;

namespace AccessoryWorld.Services
{
    public interface ITradeInService
    {
        Task<TradeIn> CreateTradeInAsync(CreateTradeInRequest request);
        Task<TradeIn> GetTradeInAsync(int id);
        Task<TradeIn> GetTradeInByPublicIdAsync(Guid publicId);
        Task<IEnumerable<TradeIn>> GetUserTradeInsAsync(string userId);
        Task<TradeIn> UpdateTradeInStatusAsync(int id, string newStatus, string? approvedBy = null, string? notes = null);
        Task<TradeIn> EvaluateTradeInAsync(int id, decimal approvedValue, string approvedBy, string? notes = null);
        Task<CreditNote> AcceptTradeInAsync(int id);
        Task<CreditNote> GetCreditNoteAsync(string creditNoteCode);
        Task<decimal> ApplyCreditNoteAsync(string creditNoteCode, int orderId, decimal amount);
        Task<IEnumerable<CreditNote>> GetUserCreditNotesAsync(string userId);
        Task ExpireCreditNotesAsync();
        Task<TradeInStatistics> GetTradeInStatisticsAsync();
    }

    public class TradeInService : ITradeInService
    {
        private readonly ApplicationDbContext _context;
        private readonly TradeInDomainService _domainService;
        private readonly ILogger<TradeInService> _logger;

        public TradeInService(
            ApplicationDbContext context, 
            TradeInDomainService domainService,
            ILogger<TradeInService> logger)
        {
            _context = context;
            _domainService = domainService;
            _logger = logger;
        }

        public async Task<TradeIn> CreateTradeInAsync(CreateTradeInRequest request)
        {
            var tradeIn = new TradeIn
            {
                PublicId = Guid.NewGuid(),
                CustomerId = request.CustomerId,
                DeviceBrand = request.DeviceBrand,
                DeviceModel = request.DeviceModel,
                DeviceType = request.DeviceType,
                IMEI = request.IMEI,
                Description = request.Description,
                ConditionGrade = request.ConditionGrade,
                PhotosJson = JsonSerializer.Serialize(request.Photos ?? new List<string>()),
                Status = TradeInDomainService.TradeInStatus.Submitted,
                ProposedValue = request.ProposedValue,
                CreatedAt = DateTime.UtcNow
            };

            // Validate domain invariants
            _domainService.ValidateTradeInInvariants(tradeIn);

            _context.TradeIns.Add(tradeIn);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new Trade-In {TradeInId} for customer {CustomerId}", 
                tradeIn.Id, request.CustomerId);

            return tradeIn;
        }

        public async Task<TradeIn> GetTradeInAsync(int id)
        {
            var tradeIn = await _context.TradeIns
                .Include(t => t.Customer)
                .Include(t => t.ApprovedByUser)
                .Include(t => t.CreditNote)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tradeIn == null)
                throw new DomainException($"Trade-In with ID {id} not found");

            return tradeIn;
        }

        public async Task<TradeIn> GetTradeInByPublicIdAsync(Guid publicId)
        {
            var tradeIn = await _context.TradeIns
                .Include(t => t.Customer)
                .Include(t => t.ApprovedByUser)
                .Include(t => t.CreditNote)
                .FirstOrDefaultAsync(t => t.PublicId == publicId);

            if (tradeIn == null)
                throw new DomainException($"Trade-In with Public ID {publicId} not found");

            return tradeIn;
        }

        public async Task<IEnumerable<TradeIn>> GetUserTradeInsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Retrieving trade-ins for user {UserId}", userId);
                
                var tradeIns = await _context.TradeIns
                    .Include(t => t.CreditNote)
                    .Where(t => t.CustomerId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
                
                _logger.LogInformation("Successfully retrieved {Count} trade-ins for user {UserId}", tradeIns.Count, userId);
                return tradeIns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trade-ins for user {UserId}. Error: {ErrorMessage}", userId, ex.Message);
                throw;
            }
        }

        public async Task<TradeIn> UpdateTradeInStatusAsync(int id, string newStatus, string? approvedBy = null, string? notes = null)
        {
            await _domainService.TransitionTradeInStatusAsync(id, newStatus, approvedBy, notes);
            return await GetTradeInAsync(id);
        }

        public async Task<TradeIn> EvaluateTradeInAsync(int id, decimal approvedValue, string approvedBy, string? notes = null)
        {
            var tradeIn = await GetTradeInAsync(id);
            
            if (tradeIn.Status != TradeInDomainService.TradeInStatus.Submitted && 
                tradeIn.Status != TradeInDomainService.TradeInStatus.UnderReview)
            {
                throw new DomainException("Trade-In must be in SUBMITTED or UNDER_REVIEW status to be evaluated");
            }

            tradeIn.ApprovedValue = approvedValue;
            await _domainService.TransitionTradeInStatusAsync(id, TradeInDomainService.TradeInStatus.Evaluated, approvedBy, notes);

            // Automatically send offer after evaluation
            await _domainService.TransitionTradeInStatusAsync(id, TradeInDomainService.TradeInStatus.OfferSent, approvedBy);

            return await GetTradeInAsync(id);
        }

        public async Task<CreditNote> AcceptTradeInAsync(int id)
        {
            var tradeIn = await GetTradeInAsync(id);
            
            if (tradeIn.Status != TradeInDomainService.TradeInStatus.OfferSent)
                throw new DomainException("Trade-In must be in OFFER_SENT status to be accepted");

            if (!tradeIn.ApprovedValue.HasValue || tradeIn.ApprovedValue.Value <= 0)
                throw new DomainException("Trade-In must have a valid approved value to be accepted");

            // Transition to accepted status
            await _domainService.TransitionTradeInStatusAsync(id, TradeInDomainService.TradeInStatus.Accepted);

            // Create credit note
            var creditNote = await _domainService.CreateCreditNoteAsync(id, tradeIn.ApprovedValue.Value);

            // Complete the trade-in
            await _domainService.TransitionTradeInStatusAsync(id, TradeInDomainService.TradeInStatus.Completed);

            _logger.LogInformation("Trade-In {TradeInId} accepted and credit note {CreditNoteCode} created", 
                id, creditNote.CreditNoteCode);

            return creditNote;
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

        public async Task<decimal> ApplyCreditNoteAsync(string creditNoteCode, int orderId, decimal amount)
        {
            return await _domainService.ApplyCreditNoteAsync(creditNoteCode, orderId, amount);
        }

        public async Task<IEnumerable<CreditNote>> GetUserCreditNotesAsync(string userId)
        {
            return await _context.CreditNotes
                .Where(cn => cn.UserId == userId)
                .Where(cn => cn.Status == TradeInDomainService.CreditNoteStatus.Active || 
                           cn.Status == TradeInDomainService.CreditNoteStatus.PartiallyUsed)
                .OrderByDescending(cn => cn.CreatedAt)
                .ToListAsync();
        }

        public async Task ExpireCreditNotesAsync()
        {
            var expiredCreditNotes = await _context.CreditNotes
                .Where(cn => cn.ExpiresAt < DateTime.UtcNow)
                .Where(cn => cn.Status == TradeInDomainService.CreditNoteStatus.Active || 
                           cn.Status == TradeInDomainService.CreditNoteStatus.PartiallyUsed)
                .ToListAsync();

            foreach (var creditNote in expiredCreditNotes)
            {
                creditNote.Status = TradeInDomainService.CreditNoteStatus.Expired;
                _logger.LogInformation("Credit note {CreditNoteCode} expired", creditNote.CreditNoteCode);
            }

            if (expiredCreditNotes.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Expired {Count} credit notes", expiredCreditNotes.Count);
            }
        }

        public async Task<TradeInStatistics> GetTradeInStatisticsAsync()
        {
            var stats = new TradeInStatistics();

            var tradeIns = await _context.TradeIns.ToListAsync();
            var creditNotes = await _context.CreditNotes.ToListAsync();

            stats.TotalTradeIns = tradeIns.Count;
            stats.PendingTradeIns = tradeIns.Count(t => t.Status == TradeInDomainService.TradeInStatus.Submitted || 
                                                      t.Status == TradeInDomainService.TradeInStatus.UnderReview);
            stats.CompletedTradeIns = tradeIns.Count(t => t.Status == TradeInDomainService.TradeInStatus.Completed);
            stats.TotalTradeInValue = tradeIns.Where(t => t.ApprovedValue.HasValue).Sum(t => t.ApprovedValue.GetValueOrDefault());

            stats.ActiveCreditNotes = creditNotes.Count(cn => cn.Status == TradeInDomainService.CreditNoteStatus.Active || 
                                                             cn.Status == TradeInDomainService.CreditNoteStatus.PartiallyUsed);
            stats.TotalCreditNoteValue = creditNotes.Sum(cn => cn.Amount);
            stats.RemainingCreditNoteValue = creditNotes.Sum(cn => cn.AmountRemaining);

            return stats;
        }
    }

    // Request/Response DTOs
    public class CreateTradeInRequest
    {
        public string CustomerId { get; set; } = string.Empty;
        public string DeviceBrand { get; set; } = string.Empty;
        public string DeviceModel { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string? IMEI { get; set; }
        public string? Description { get; set; }
        public string ConditionGrade { get; set; } = string.Empty;
        public List<string>? Photos { get; set; }
        public decimal? ProposedValue { get; set; }
    }

    public class TradeInStatistics
    {
        public int TotalTradeIns { get; set; }
        public int PendingTradeIns { get; set; }
        public int CompletedTradeIns { get; set; }
        public decimal TotalTradeInValue { get; set; }
        public int ActiveCreditNotes { get; set; }
        public decimal TotalCreditNoteValue { get; set; }
        public decimal RemainingCreditNoteValue { get; set; }
    }
}