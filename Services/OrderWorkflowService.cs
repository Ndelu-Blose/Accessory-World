using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccessoryWorld.Services
{
    public interface IOrderWorkflowService
    {
        Task<Order> ProcessPaymentSuccessAsync(int orderId, string transactionId);
        Task<Order> StartOrderProcessingAsync(int orderId);
        Task<Order> ShipOrderAsync(int orderId, string courierCode, string? trackingNumber = null);
        Task<Order> DeliverOrderAsync(int orderId, string? proofOfDelivery = null);
        Task<Order> CancelOrderAsync(int orderId, string reason);
        Task<Order> RefundOrderAsync(int orderId, decimal refundAmount, string reason);
        Task<PickupOTP> GeneratePickupOTPAsync(int orderId);
        Task<Order> ProcessPickupAsync(int orderId, string otpCode, string staffId);
        Task<bool> ValidateOrderWorkflowAsync(int orderId);
        Task<List<string>> GetAvailableActionsAsync(int orderId);
    }

    public class OrderWorkflowService : IOrderWorkflowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWorkflowValidationService _workflowValidation;
        private readonly ILogger<OrderWorkflowService> _logger;

        public OrderWorkflowService(
            ApplicationDbContext context,
            IWorkflowValidationService workflowValidation,
            ILogger<OrderWorkflowService> logger)
        {
            _context = context;
            _workflowValidation = workflowValidation;
            _logger = logger;
        }

        public async Task<Order> ProcessPaymentSuccessAsync(int orderId, string transactionId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Processing payment success for order {OrderId}, transaction {TransactionId}", 
                    orderId, transactionId);

                // Update order status to PAID with concurrency control
                var order = await _workflowValidation.UpdateOrderStatusWithConcurrencyAsync(
                    orderId, OrderStatus.Paid, $"Payment successful - Transaction: {transactionId}");

                // Update order items to CONFIRMED
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    if (await _workflowValidation.ValidateOrderItemStateTransitionAsync(item.Id, "CONFIRMED"))
                    {
                        item.Status = "CONFIRMED";
                    }
                }

                // Reserve stock for confirmed items
                await ReserveStockAsync(orderItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Payment processing completed for order {OrderId}", orderId);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing payment success for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order> StartOrderProcessingAsync(int orderId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Starting order processing for order {OrderId}", orderId);

                // Update order status to PROCESSING
                var order = await _workflowValidation.UpdateOrderStatusWithConcurrencyAsync(
                    orderId, OrderStatus.Processing, "Order processing started");

                // Validate and allocate stock
                await AllocateStockAsync(orderId);

                // Create shipment record if delivery method
                if (order.FulfilmentMethod == "DELIVERY")
                {
                    await CreateShipmentRecordAsync(orderId);
                }
                // Create pickup OTP if pickup method
                else if (order.FulfilmentMethod == "PICKUP")
                {
                    await GeneratePickupOTPAsync(orderId);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order processing started for order {OrderId}", orderId);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error starting order processing for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order> ShipOrderAsync(int orderId, string courierCode, string? trackingNumber = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Shipping order {OrderId} with courier {CourierCode}", orderId, courierCode);

                // Update order status to SHIPPED
                var order = await _workflowValidation.UpdateOrderStatusWithConcurrencyAsync(
                    orderId, OrderStatus.Shipped, $"Shipped via {courierCode}");

                // Update shipment record
                var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId);
                if (shipment != null)
                {
                    if (await _workflowValidation.ValidateShipmentStateTransitionAsync(shipment.Id, "IN_TRANSIT"))
                    {
                        shipment.Status = "IN_TRANSIT";
                        shipment.CourierCode = courierCode;
                        shipment.TrackingNumber = trackingNumber;
                        shipment.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Update order items to FULFILLED
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    if (await _workflowValidation.ValidateOrderItemStateTransitionAsync(item.Id, "FULFILLED"))
                    {
                        item.Status = "FULFILLED";
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderId} shipped successfully", orderId);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error shipping order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order> DeliverOrderAsync(int orderId, string? proofOfDelivery = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Delivering order {OrderId}", orderId);

                // Update order status to DELIVERED
                var order = await _workflowValidation.UpdateOrderStatusWithConcurrencyAsync(
                    orderId, OrderStatus.Delivered, "Order delivered");

                // Update shipment record
                var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId);
                if (shipment != null)
                {
                    if (await _workflowValidation.ValidateShipmentStateTransitionAsync(shipment.Id, "DELIVERED"))
                    {
                        shipment.Status = "DELIVERED";
                        shipment.ActualDeliveryDate = DateTime.UtcNow;
                        shipment.ProofOfDelivery = proofOfDelivery;
                        shipment.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderId} delivered successfully", orderId);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error delivering order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order> CancelOrderAsync(int orderId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Cancelling order {OrderId} with reason: {Reason}", orderId, reason);

                // Validate cancellation is allowed
                if (!await _workflowValidation.CanCancelOrderAsync(orderId))
                {
                    throw new DomainException("Order cannot be cancelled at this time", DomainErrors.INVALID_ORDER_STATE);
                }

                // Update order status to CANCELLED
                var order = await _workflowValidation.UpdateOrderStatusWithConcurrencyAsync(
                    orderId, OrderStatus.Cancelled, reason);

                // Release reserved/allocated stock
                await ReleaseStockAsync(orderId);

                // Cancel shipment if exists
                var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId);
                if (shipment != null)
                {
                    if (await _workflowValidation.ValidateShipmentStateTransitionAsync(shipment.Id, "CANCELLED"))
                    {
                        shipment.Status = "CANCELLED";
                        shipment.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Cancel order items
                var orderItems = await _context.OrderItems
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    if (await _workflowValidation.ValidateOrderItemStateTransitionAsync(item.Id, "CANCELLED"))
                    {
                        item.Status = "CANCELLED";
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Order {OrderId} cancelled successfully", orderId);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order> RefundOrderAsync(int orderId, decimal refundAmount, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Processing refund for order {OrderId}, amount {RefundAmount}", orderId, refundAmount);

                // Validate refund is allowed
                if (!await _workflowValidation.CanRefundOrderAsync(orderId))
                {
                    throw new DomainException("Order cannot be refunded at this time", DomainErrors.INVALID_ORDER_STATE);
                }

                // Update order status to REFUNDED
                var order = await _workflowValidation.UpdateOrderStatusWithConcurrencyAsync(
                    orderId, OrderStatus.Refunded, reason);

                // Create refund payment record
                var refundPayment = new Payment
                {
                    OrderId = orderId,
                    Method = "REFUND",
                    Amount = -refundAmount, // Negative amount for refund
                    Currency = "ZAR",
                    Status = "SUCCEEDED",
                    PaymentIntentId = $"REFUND_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    ProcessedAt = DateTime.UtcNow
                };

                _context.Payments.Add(refundPayment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Refund processed for order {OrderId}", orderId);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing refund for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<PickupOTP> GeneratePickupOTPAsync(int orderId)
        {
            try
            {
                // Check if OTP already exists and is active
                var existingOTP = await _context.PickupOTPs
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status == "ACTIVE");

                if (existingOTP != null && existingOTP.ExpiresAt > DateTime.UtcNow)
                {
                    return existingOTP; // Return existing valid OTP
                }

                // Expire existing OTP if any
                if (existingOTP != null)
                {
                    if (await _workflowValidation.ValidatePickupOTPStateTransitionAsync(existingOTP.Id, "EXPIRED"))
                    {
                        existingOTP.Status = "EXPIRED";
                    }
                }

                // Generate new OTP
                var otpCode = GenerateOTPCode();
                var newOTP = new PickupOTP
                {
                    OrderId = orderId,
                    OTPCode = otpCode,
                    Status = "ACTIVE",
                    ExpiresAt = DateTime.UtcNow.AddHours(72), // 72-hour validity
                    CreatedAt = DateTime.UtcNow
                };

                _context.PickupOTPs.Add(newOTP);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Generated pickup OTP for order {OrderId}", orderId);
                return newOTP;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating pickup OTP for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Order> ProcessPickupAsync(int orderId, string otpCode, string staffId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Processing pickup for order {OrderId} by staff {StaffId}", orderId, staffId);

                // Validate OTP
                var otp = await _context.PickupOTPs
                    .FirstOrDefaultAsync(p => p.OrderId == orderId && p.OTPCode == otpCode && p.Status == "ACTIVE");

                if (otp == null || otp.ExpiresAt < DateTime.UtcNow)
                {
                    throw new DomainException("Invalid or expired pickup OTP", DomainErrors.INVALID_ORDER_STATE);
                }

                // Update OTP status to USED
                if (await _workflowValidation.ValidatePickupOTPStateTransitionAsync(otp.Id, "USED"))
                {
                    otp.Status = "USED";
                    otp.UsedAt = DateTime.UtcNow;
                    otp.UsedByStaffId = staffId;
                }

                // Update order status to DELIVERED (pickup completed)
                var order = await _workflowValidation.UpdateOrderStatusWithConcurrencyAsync(
                    orderId, OrderStatus.Delivered, $"Picked up by customer - Staff: {staffId}");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Pickup processed successfully for order {OrderId}", orderId);
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing pickup for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> ValidateOrderWorkflowAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Payments)
                    .Include(o => o.Shipment)
                    .Include(o => o.PickupOTP)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return false;

                // Validate order state consistency
                var currentStatus = Enum.Parse<OrderStatus>(order.Status, true);
                
                switch (currentStatus)
                {
                    case OrderStatus.Paid:
                        // Must have successful payment
                        return order.Payments.Any(p => p.Status == "SUCCEEDED");
                    
                    case OrderStatus.Processing:
                        // Must be paid and have confirmed items
                        return order.Payments.Any(p => p.Status == "SUCCEEDED") &&
                               order.OrderItems.All(oi => oi.Status == "CONFIRMED");
                    
                    case OrderStatus.Shipped:
                        // Must have shipment record for delivery orders
                        if (order.FulfilmentMethod == "DELIVERY")
                            return order.Shipment != null && order.Shipment.Status == "IN_TRANSIT";
                        return true;
                    
                    case OrderStatus.Delivered:
                        // Must have delivery proof or pickup confirmation
                        if (order.FulfilmentMethod == "DELIVERY")
                            return order.Shipment?.Status == "DELIVERED";
                        else if (order.FulfilmentMethod == "PICKUP")
                            return order.PickupOTP?.Status == "USED";
                        return false;
                    
                    default:
                        return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating order workflow for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<List<string>> GetAvailableActionsAsync(int orderId)
        {
            var actions = new List<string>();
            
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return actions;

                var currentStatus = Enum.Parse<OrderStatus>(order.Status, true);
                
                switch (currentStatus)
                {
                    case OrderStatus.Pending:
                        actions.AddRange(new[] { "Cancel", "ProcessPayment" });
                        break;
                    
                    case OrderStatus.Paid:
                        actions.AddRange(new[] { "StartProcessing", "Cancel" });
                        break;
                    
                    case OrderStatus.Processing:
                        if (order.FulfilmentMethod == "DELIVERY")
                            actions.Add("Ship");
                        else if (order.FulfilmentMethod == "PICKUP")
                            actions.Add("GenerateOTP");
                        actions.Add("Cancel");
                        break;
                    
                    case OrderStatus.Shipped:
                        actions.AddRange(new[] { "MarkDelivered", "Cancel" });
                        break;
                    
                    case OrderStatus.Delivered:
                        actions.Add("Refund");
                        break;
                    
                    case OrderStatus.Cancelled:
                        actions.Add("Refund");
                        break;
                }

                return actions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available actions for order {OrderId}", orderId);
                return actions;
            }
        }

        private async Task ReserveStockAsync(List<OrderItem> orderItems)
        {
            foreach (var item in orderItems)
            {
                var sku = await _context.SKUs.FindAsync(item.SKUId);
                if (sku != null)
                {
                    sku.ReservedQuantity += item.Quantity;
                }
            }
        }

        private async Task AllocateStockAsync(int orderId)
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.SKU)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                // Move from reserved to allocated (reduce actual stock)
                item.SKU.StockQuantity -= item.Quantity;
                item.SKU.ReservedQuantity -= item.Quantity;
            }
        }

        private async Task ReleaseStockAsync(int orderId)
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.SKU)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                // Release reserved stock back to available
                if (item.Status == "CONFIRMED")
                {
                    item.SKU.ReservedQuantity -= item.Quantity;
                }
                // If already allocated (processing/shipped), return to stock
                else if (item.Status == "FULFILLED")
                {
                    item.SKU.StockQuantity += item.Quantity;
                }
            }
        }

        private async Task CreateShipmentRecordAsync(int orderId)
        {
            var existingShipment = await _context.Shipments.FirstOrDefaultAsync(s => s.OrderId == orderId);
            if (existingShipment == null)
            {
                var shipment = new Shipment
                {
                    OrderId = orderId,
                    CourierCode = "TBD", // To be determined when shipping
                    Status = "PREPARING",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Shipments.Add(shipment);
            }
        }

        private string GenerateOTPCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString(); // 6-digit OTP
        }
    }
}