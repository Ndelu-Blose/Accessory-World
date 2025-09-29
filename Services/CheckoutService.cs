using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace AccessoryWorld.Services
{
    public interface ICheckoutService
    {
        Task<CheckoutSession> CreateCheckoutSessionAsync(string userId, string? creditNoteCode = null, decimal? creditNoteAmount = null);
        Task<CheckoutSession> GetCheckoutSessionAsync(Guid sessionId);
        Task<bool> ValidateCreditNoteForCheckoutAsync(string creditNoteCode, decimal requestedAmount, string userId);
        Task<decimal> ApplyCreditNoteToCheckoutAsync(Guid sessionId, string creditNoteCode, decimal requestedAmount);
        Task<Order> ProcessOrderAsync(Guid sessionId, string userId, Guid shippingAddressId, string fulfillmentMethod, string? notes);
        Task<bool> LockOrderItemsAsync(List<int> skuIds, List<int> quantities, Guid sessionId);
        Task<bool> LockCreditNoteAsync(string creditNoteCode, decimal amount, Guid sessionId);
        Task ReleaseLockAsync(Guid sessionId);
        Task ReleaseCreditNoteLockAsync(Guid sessionId);
        Task<bool> ValidateStockAvailabilityAsync(List<CartItem> cartItems);
    }

    public class CheckoutService : ICheckoutService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICreditNoteService _creditNoteService;
        private readonly IOrderService _orderService;
        private readonly ILogger<CheckoutService> _logger;
        private readonly IMemoryCache _cache;

        public CheckoutService(
            ApplicationDbContext context,
            ICreditNoteService creditNoteService,
            IOrderService orderService,
            ILogger<CheckoutService> logger,
            IMemoryCache cache)
        {
            _context = context;
            _creditNoteService = creditNoteService;
            _orderService = orderService;
            _logger = logger;
            _cache = cache;
        }

        public async Task<CheckoutSession> CreateCheckoutSessionAsync(string userId, string? creditNoteCode = null, decimal? creditNoteAmount = null)
        {
            var session = new CheckoutSession
            {
                SessionId = Guid.NewGuid(),
                UserId = userId,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30), // 30-minute session timeout
                AppliedCreditNoteCode = creditNoteCode,
                CreditNoteAmount = creditNoteAmount ?? 0
            };

            _context.CheckoutSessions.Add(session);
            await _context.SaveChangesAsync();

            // Lock credit note if provided
            if (!string.IsNullOrEmpty(creditNoteCode) && creditNoteAmount.HasValue && creditNoteAmount.Value > 0)
            {
                var lockSuccess = await LockCreditNoteAsync(creditNoteCode, creditNoteAmount.Value, session.SessionId);
                if (!lockSuccess)
                {
                    // Remove the session if credit note locking fails
                    _context.CheckoutSessions.Remove(session);
                    await _context.SaveChangesAsync();
                    throw new DomainException("Credit note is currently locked by another session");
                }
            }

            _logger.LogInformation("Created checkout session {SessionId} for user {UserId}", session.SessionId, userId);
            return session;
        }

        public async Task<CheckoutSession> GetCheckoutSessionAsync(Guid sessionId)
        {
            var session = await _context.CheckoutSessions
                .Include(s => s.StockLocks)
                .FirstOrDefaultAsync(s => s.SessionId == sessionId);

            if (session == null)
                throw new DomainException("Checkout session not found");

            if (session.ExpiresAt < DateTime.UtcNow)
            {
                session.Status = "EXPIRED";
                await _context.SaveChangesAsync();
                throw new DomainException("Checkout session has expired");
            }

            return session;
        }

        public async Task<bool> ValidateCreditNoteForCheckoutAsync(string creditNoteCode, decimal requestedAmount, string userId)
        {
            try
            {
                var creditNote = await _creditNoteService.ValidateCreditNoteAsync(creditNoteCode, userId);
                return creditNote != null && creditNote.AmountRemaining >= requestedAmount;
            }
            catch
            {
                return false;
            }
        }

        public async Task<decimal> ApplyCreditNoteToCheckoutAsync(Guid sessionId, string creditNoteCode, decimal requestedAmount)
        {
            var session = await GetCheckoutSessionAsync(sessionId);
            
            var creditNote = await _creditNoteService.ValidateCreditNoteAsync(creditNoteCode, session.UserId);
            if (creditNote == null)
                throw new DomainException("Invalid credit note");

            var applicableAmount = Math.Min(creditNote.AmountRemaining, requestedAmount);
            
            session.AppliedCreditNoteCode = creditNoteCode;
            session.CreditNoteAmount = applicableAmount;
            
            await _context.SaveChangesAsync();
            
            return applicableAmount;
        }

        public async Task<Order> ProcessOrderAsync(Guid sessionId, string userId, Guid shippingAddressId, string fulfillmentMethod, string? notes)
        {
            var session = await GetCheckoutSessionAsync(sessionId);
            
            if (session.UserId != userId)
                throw new DomainException("Invalid session for user");

            // Create the order through OrderService with credit note amount
            var creditNoteAmount = session.CreditNoteAmount;
            var order = await _orderService.CreateOrderAsync(userId, shippingAddressId, fulfillmentMethod, notes, creditNoteAmount);

            // Apply credit note if present
            if (!string.IsNullOrEmpty(session.AppliedCreditNoteCode) && session.CreditNoteAmount > 0)
            {
                await _creditNoteService.ApplyCreditNoteAsync(session.AppliedCreditNoteCode, session.CreditNoteAmount, order.Id);
                _logger.LogInformation("Applied credit note {CreditNoteCode} amount {Amount} to order {OrderId}", 
                    session.AppliedCreditNoteCode, session.CreditNoteAmount, order.Id);
            }

            // Mark session as completed
            session.Status = "COMPLETED";
            session.CompletedAt = DateTime.UtcNow;
            
            // Release any stock locks
            await ReleaseLockAsync(sessionId);
            
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<bool> LockOrderItemsAsync(List<int> skuIds, List<int> quantities, Guid sessionId)
        {
            var session = await GetCheckoutSessionAsync(sessionId);
            
            for (int i = 0; i < skuIds.Count; i++)
            {
                var stockLock = new StockLock
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    SKUId = skuIds[i],
                    Quantity = quantities[i],
                    Status = "LOCKED",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                };

                _context.StockLocks.Add(stockLock);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ReleaseLockAsync(Guid sessionId)
        {
            var locks = await _context.StockLocks
                .Where(l => l.SessionId == sessionId && l.Status == "LOCKED")
                .ToListAsync();

            foreach (var lockItem in locks)
            {
                lockItem.Status = "RELEASED";
                lockItem.ReleasedAt = DateTime.UtcNow;
            }

            // Also release credit note locks
            await ReleaseCreditNoteLockAsync(sessionId);

            await _context.SaveChangesAsync();
        }

        public async Task<bool> LockCreditNoteAsync(string creditNoteCode, decimal amount, Guid sessionId)
        {
            // Check if credit note is already locked by another session
            var existingLock = await _context.CreditNoteLocks
                .Where(l => l.CreditNoteCode == creditNoteCode && l.Status == "LOCKED")
                .Where(l => l.SessionId != sessionId) // Allow same session to update lock
                .FirstOrDefaultAsync();

            if (existingLock != null)
            {
                _logger.LogWarning("Credit note {CreditNoteCode} is already locked by session {SessionId}", 
                    creditNoteCode, existingLock.SessionId);
                return false;
            }

            // Check if we already have a lock for this session and credit note
            var sessionLock = await _context.CreditNoteLocks
                .FirstOrDefaultAsync(l => l.SessionId == sessionId && l.CreditNoteCode == creditNoteCode);

            if (sessionLock != null)
            {
                // Update existing lock
                sessionLock.LockedAmount = amount;
                sessionLock.ExpiresAt = DateTime.UtcNow.AddMinutes(30);
            }
            else
            {
                // Create new lock
                var creditNoteLock = new CreditNoteLock
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    CreditNoteCode = creditNoteCode,
                    LockedAmount = amount,
                    Status = "LOCKED",
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(30)
                };

                _context.CreditNoteLocks.Add(creditNoteLock);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task ReleaseCreditNoteLockAsync(Guid sessionId)
        {
            var locks = await _context.CreditNoteLocks
                .Where(l => l.SessionId == sessionId && l.Status == "LOCKED")
                .ToListAsync();

            foreach (var lockItem in locks)
            {
                lockItem.Status = "RELEASED";
                lockItem.ReleasedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ValidateStockAvailabilityAsync(List<CartItem> cartItems)
        {
            foreach (var item in cartItems)
            {
                var sku = await _context.SKUs.FindAsync(item.SKUId);
                if (sku == null || sku.StockQuantity < item.Quantity)
                {
                    return false;
                }
            }
            return true;
        }
    }
}