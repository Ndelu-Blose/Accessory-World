using AccessoryWorld.Data;
using AccessoryWorld.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AccessoryWorld.Services
{
    public interface IPaymentValidationService
    {
        Task<ValidationResult> ValidatePaymentRequestAsync(Order order, string paymentMethod);
        Task<ValidationResult> ValidatePaymentAmountAsync(int orderId, decimal amount);
        Task<bool> IsPaymentDuplicateAsync(string transactionId);
        Task<ValidationResult> ValidatePaymentTimeoutAsync(int orderId);
        Task<bool> ReconcilePaymentAsync(int orderId);
        Task<ValidationResult> ValidateRefundRequestAsync(int paymentId, decimal refundAmount, string reason);
    }

    public class PaymentValidationService : IPaymentValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PaymentValidationService> _logger;
        private readonly IConfiguration _configuration;

        public PaymentValidationService(
            ApplicationDbContext context,
            ILogger<PaymentValidationService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ValidationResult> ValidatePaymentRequestAsync(Order order, string paymentMethod)
        {
            var errors = new List<string>();

            // 1. Validate order exists and is in correct state
            if (order == null)
            {
                errors.Add("Order not found");
                return new ValidationResult(false, errors);
            }

            // 2. Validate order status allows payment
            var validPaymentStatuses = new[] { "PENDING", "PENDING_PAYMENT", "AWAITING_PAYMENT" };
            if (!validPaymentStatuses.Contains(order.Status.ToUpper()))
            {
                errors.Add($"Order status '{order.Status}' does not allow payment");
            }

            // 3. Validate order total is positive
            if (order.Total <= 0)
            {
                errors.Add("Order total must be greater than zero");
            }

            // 4. Validate payment method
            var supportedMethods = new[] { "PAYFAST", "EFT", "CARD" };
            if (!supportedMethods.Contains(paymentMethod.ToUpper()))
            {
                errors.Add($"Payment method '{paymentMethod}' is not supported");
            }

            // 5. Check for existing successful payments
            var existingPayment = await _context.Payments
                .Where(p => p.OrderId == order.Id && p.Status == "COMPLETED")
                .FirstOrDefaultAsync();

            if (existingPayment != null)
            {
                errors.Add("Order has already been paid");
            }

            // 6. Validate order items are still available
            var orderItems = await _context.OrderItems
                .Include(oi => oi.SKU)
                .Where(oi => oi.OrderId == order.Id)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                if (item.SKU.StockQuantity < item.Quantity)
                {
                    errors.Add($"Insufficient stock for {item.SKU.Variant}. Available: {item.SKU.StockQuantity}, Required: {item.Quantity}");
                }
            }

            // 7. Validate order age (prevent payment on very old orders)
            var maxOrderAge = _configuration.GetValue<int>("Payment:MaxOrderAgeHours", 72); // Default 72 hours
            if (order.CreatedAt.AddHours(maxOrderAge) < DateTime.UtcNow)
            {
                errors.Add($"Order is too old to process payment. Maximum age: {maxOrderAge} hours");
            }

            return new ValidationResult(errors.Count == 0, errors);
        }

        public async Task<ValidationResult> ValidatePaymentAmountAsync(int orderId, decimal amount)
        {
            var errors = new List<string>();

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                errors.Add("Order not found");
                return new ValidationResult(false, errors);
            }

            // Allow for small rounding differences (1 cent)
            var tolerance = 0.01m;
            if (Math.Abs(amount - order.Total) > tolerance)
            {
                errors.Add($"Payment amount mismatch. Expected: {order.Total:F2}, Received: {amount:F2}");
            }

            // Validate amount is positive
            if (amount <= 0)
            {
                errors.Add("Payment amount must be greater than zero");
            }

            // Validate amount is reasonable (not too large)
            var maxPaymentAmount = _configuration.GetValue<decimal>("Payment:MaxAmount", 100000m);
            if (amount > maxPaymentAmount)
            {
                errors.Add($"Payment amount exceeds maximum allowed: {maxPaymentAmount:F2}");
            }

            return new ValidationResult(errors.Count == 0, errors);
        }

        public async Task<bool> IsPaymentDuplicateAsync(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId))
                return false;

            return await _context.Payments
                .AnyAsync(p => p.TransactionId == transactionId);
        }

        public async Task<ValidationResult> ValidatePaymentTimeoutAsync(int orderId)
        {
            var errors = new List<string>();

            var pendingPayments = await _context.Payments
                .Where(p => p.OrderId == orderId && p.Status == "PENDING")
                .ToListAsync();

            var paymentTimeoutMinutes = _configuration.GetValue<int>("Payment:TimeoutMinutes", 30);

            foreach (var payment in pendingPayments)
            {
                if (payment.CreatedAt.AddMinutes(paymentTimeoutMinutes) < DateTime.UtcNow)
                {
                    // Mark payment as timed out
                    payment.Status = "FAILED";
                    payment.FailureReason = "Payment timeout";
                    payment.ProcessedAt = DateTime.UtcNow;
                    
                    _logger.LogWarning($"Payment {payment.Id} timed out for order {orderId}");
                }
            }

            if (pendingPayments.Any(p => p.Status == "FAILED"))
            {
                await _context.SaveChangesAsync();
                errors.Add("One or more payments have timed out");
            }

            return new ValidationResult(errors.Count == 0, errors);
        }

        public async Task<bool> ReconcilePaymentAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Payments)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                    return false;

                var completedPayments = order.Payments
                    .Where(p => p.Status == "COMPLETED")
                    .ToList();

                var totalPaid = completedPayments.Sum(p => p.Amount);
                var tolerance = 0.01m;

                // Check if order is fully paid
                if (Math.Abs(totalPaid - order.Total) <= tolerance && totalPaid >= order.Total)
                {
                    if (order.Status != "PAID")
                    {
                        order.Status = "PAID";
                        order.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Order {orderId} reconciled and marked as PAID. Total paid: {totalPaid:F2}");
                    }
                    return true;
                }

                // Check for overpayment
                if (totalPaid > order.Total + tolerance)
                {
                    _logger.LogWarning($"Overpayment detected for order {orderId}. Expected: {order.Total:F2}, Paid: {totalPaid:F2}");
                    // Could trigger refund process here
                }

                // Check for underpayment
                if (totalPaid < order.Total - tolerance)
                {
                    _logger.LogInformation($"Partial payment for order {orderId}. Expected: {order.Total:F2}, Paid: {totalPaid:F2}");
                    order.Status = "PARTIAL_PAYMENT";
                    order.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reconciling payment for order {orderId}");
                return false;
            }
        }

        public async Task<ValidationResult> ValidateRefundRequestAsync(int paymentId, decimal refundAmount, string reason)
        {
            var errors = new List<string>();

            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
            {
                errors.Add("Payment not found");
                return new ValidationResult(false, errors);
            }

            // Validate payment status allows refund
            if (payment.Status != "COMPLETED")
            {
                errors.Add($"Payment status '{payment.Status}' does not allow refund");
            }

            // Validate refund amount
            if (refundAmount <= 0)
            {
                errors.Add("Refund amount must be greater than zero");
            }

            var maxRefundAmount = payment.Amount - payment.RefundedAmount;
            if (refundAmount > maxRefundAmount)
            {
                errors.Add($"Refund amount exceeds available amount. Maximum: {maxRefundAmount:F2}");
            }

            // Validate refund reason
            if (string.IsNullOrWhiteSpace(reason))
            {
                errors.Add("Refund reason is required");
            }

            // Validate refund timeframe
            var maxRefundDays = _configuration.GetValue<int>("Payment:MaxRefundDays", 30);
            if (payment.ProcessedAt?.AddDays(maxRefundDays) < DateTime.UtcNow)
            {
                errors.Add($"Refund request exceeds maximum timeframe of {maxRefundDays} days");
            }

            return new ValidationResult(errors.Count == 0, errors);
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public List<string> Errors { get; }

        public ValidationResult(bool isValid, List<string> errors)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
        }

        public ValidationResult(bool isValid, string error = "")
        {
            IsValid = isValid;
            Errors = string.IsNullOrEmpty(error) ? new List<string>() : new List<string> { error };
        }
    }
}