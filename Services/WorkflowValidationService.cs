using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccessoryWorld.Services
{
    public interface IWorkflowValidationService
    {
        Task<bool> ValidateOrderStateTransitionAsync(int orderId, OrderStatus newStatus, string? reason = null);
        Task<bool> ValidateOrderItemStateTransitionAsync(int orderItemId, string newStatus);
        Task<bool> ValidateShipmentStateTransitionAsync(int shipmentId, string newStatus);
        Task<bool> ValidatePaymentStateTransitionAsync(int paymentId, string newStatus);
        Task<bool> ValidatePickupOTPStateTransitionAsync(int otpId, string newStatus);
        Task<Order> UpdateOrderStatusWithConcurrencyAsync(int orderId, OrderStatus newStatus, string? reason = null);
        Task ValidateBusinessRulesAsync(Order order, OrderStatus newStatus);
        Task<bool> CanCancelOrderAsync(int orderId);
        Task<bool> CanRefundOrderAsync(int orderId);
        Task<bool> IsOrderInTerminalStateAsync(int orderId);
    }

    public class WorkflowValidationService : IWorkflowValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WorkflowValidationService> _logger;
        private readonly OrderValidationService _orderValidationService;

        // Define valid state transitions for different entities
        private static readonly Dictionary<OrderStatus, List<OrderStatus>> OrderTransitions = new()
        {
            { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.Paid, OrderStatus.Cancelled } },
            { OrderStatus.Paid, new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Cancelled } },
            { OrderStatus.Processing, new List<OrderStatus> { OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.Cancelled } },
            { OrderStatus.Shipped, new List<OrderStatus> { OrderStatus.Delivered, OrderStatus.Cancelled } },
            { OrderStatus.Delivered, new List<OrderStatus> { OrderStatus.Refunded } },
            { OrderStatus.Cancelled, new List<OrderStatus> { OrderStatus.Refunded } },
            { OrderStatus.Refunded, new List<OrderStatus>() } // Terminal state
        };

        private static readonly Dictionary<string, List<string>> OrderItemTransitions = new()
        {
            { "PENDING", new List<string> { "CONFIRMED", "CANCELLED" } },
            { "CONFIRMED", new List<string> { "FULFILLED", "CANCELLED" } },
            { "FULFILLED", new List<string> { "REFUNDED" } },
            { "CANCELLED", new List<string> { "REFUNDED" } },
            { "REFUNDED", new List<string>() } // Terminal state
        };

        private static readonly Dictionary<string, List<string>> ShipmentTransitions = new()
        {
            { "PREPARING", new List<string> { "READY_FOR_DISPATCH", "CANCELLED" } },
            { "READY_FOR_DISPATCH", new List<string> { "IN_TRANSIT", "CANCELLED" } },
            { "IN_TRANSIT", new List<string> { "DELIVERED", "EXCEPTION" } },
            { "DELIVERED", new List<string>() }, // Terminal state
            { "EXCEPTION", new List<string> { "IN_TRANSIT", "CANCELLED" } },
            { "CANCELLED", new List<string>() } // Terminal state
        };

        private static readonly Dictionary<string, List<string>> PaymentTransitions = new()
        {
            { "PENDING", new List<string> { "SUCCEEDED", "FAILED", "CANCELLED" } },
            { "SUCCEEDED", new List<string> { "REFUNDED" } },
            { "FAILED", new List<string> { "PENDING" } }, // Allow retry
            { "CANCELLED", new List<string>() }, // Terminal state
            { "REFUNDED", new List<string>() } // Terminal state
        };

        private static readonly Dictionary<string, List<string>> PickupOTPTransitions = new()
        {
            { "ACTIVE", new List<string> { "USED", "EXPIRED" } },
            { "USED", new List<string>() }, // Terminal state
            { "EXPIRED", new List<string> { "ACTIVE" } } // Allow regeneration
        };

        public WorkflowValidationService(
            ApplicationDbContext context,
            ILogger<WorkflowValidationService> logger,
            OrderValidationService orderValidationService)
        {
            _context = context;
            _logger = logger;
            _orderValidationService = orderValidationService;
        }

        public async Task<bool> ValidateOrderStateTransitionAsync(int orderId, OrderStatus newStatus, string? reason = null)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for state transition validation", orderId);
                    return false;
                }

                var currentStatus = Enum.Parse<OrderStatus>(order.Status, true);
                
                // Check if transition is valid
                if (!OrderTransitions.ContainsKey(currentStatus) || 
                    !OrderTransitions[currentStatus].Contains(newStatus))
                {
                    _logger.LogWarning("Invalid order state transition from {CurrentStatus} to {NewStatus} for order {OrderId}", 
                        currentStatus, newStatus, orderId);
                    return false;
                }

                // Validate business rules
                await ValidateBusinessRulesAsync(order, newStatus);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating order state transition for order {OrderId}", orderId);
                return false;
            }
        }

        public async Task<bool> ValidateOrderItemStateTransitionAsync(int orderItemId, string newStatus)
        {
            try
            {
                var orderItem = await _context.OrderItems.FindAsync(orderItemId);
                if (orderItem == null)
                {
                    _logger.LogWarning("OrderItem {OrderItemId} not found for state transition validation", orderItemId);
                    return false;
                }

                var currentStatus = orderItem.Status.ToUpper();
                var targetStatus = newStatus.ToUpper();

                if (!OrderItemTransitions.ContainsKey(currentStatus) ||
                    !OrderItemTransitions[currentStatus].Contains(targetStatus))
                {
                    _logger.LogWarning("Invalid order item state transition from {CurrentStatus} to {NewStatus} for item {OrderItemId}",
                        currentStatus, targetStatus, orderItemId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating order item state transition for item {OrderItemId}", orderItemId);
                return false;
            }
        }

        public async Task<bool> ValidateShipmentStateTransitionAsync(int shipmentId, string newStatus)
        {
            try
            {
                var shipment = await _context.Shipments.FindAsync(shipmentId);
                if (shipment == null)
                {
                    _logger.LogWarning("Shipment {ShipmentId} not found for state transition validation", shipmentId);
                    return false;
                }

                var currentStatus = shipment.Status.ToUpper();
                var targetStatus = newStatus.ToUpper();

                if (!ShipmentTransitions.ContainsKey(currentStatus) ||
                    !ShipmentTransitions[currentStatus].Contains(targetStatus))
                {
                    _logger.LogWarning("Invalid shipment state transition from {CurrentStatus} to {NewStatus} for shipment {ShipmentId}",
                        currentStatus, targetStatus, shipmentId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating shipment state transition for shipment {ShipmentId}", shipmentId);
                return false;
            }
        }

        public async Task<bool> ValidatePaymentStateTransitionAsync(int paymentId, string newStatus)
        {
            try
            {
                var payment = await _context.Payments.FindAsync(paymentId);
                if (payment == null)
                {
                    _logger.LogWarning("Payment {PaymentId} not found for state transition validation", paymentId);
                    return false;
                }

                var currentStatus = payment.Status.ToUpper();
                var targetStatus = newStatus.ToUpper();

                if (!PaymentTransitions.ContainsKey(currentStatus) ||
                    !PaymentTransitions[currentStatus].Contains(targetStatus))
                {
                    _logger.LogWarning("Invalid payment state transition from {CurrentStatus} to {NewStatus} for payment {PaymentId}",
                        currentStatus, targetStatus, paymentId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment state transition for payment {PaymentId}", paymentId);
                return false;
            }
        }

        public async Task<bool> ValidatePickupOTPStateTransitionAsync(int otpId, string newStatus)
        {
            try
            {
                var otp = await _context.PickupOTPs.FindAsync(otpId);
                if (otp == null)
                {
                    _logger.LogWarning("PickupOTP {OtpId} not found for state transition validation", otpId);
                    return false;
                }

                var currentStatus = otp.Status.ToUpper();
                var targetStatus = newStatus.ToUpper();

                if (!PickupOTPTransitions.ContainsKey(currentStatus) ||
                    !PickupOTPTransitions[currentStatus].Contains(targetStatus))
                {
                    _logger.LogWarning("Invalid pickup OTP state transition from {CurrentStatus} to {NewStatus} for OTP {OtpId}",
                        currentStatus, targetStatus, otpId);
                    return false;
                }

                // Additional validation for OTP expiry
                if (targetStatus == "USED" && otp.ExpiresAt < DateTime.UtcNow)
                {
                    _logger.LogWarning("Cannot use expired OTP {OtpId}", otpId);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating pickup OTP state transition for OTP {OtpId}", otpId);
                return false;
            }
        }

        public async Task<Order> UpdateOrderStatusWithConcurrencyAsync(int orderId, OrderStatus newStatus, string? reason = null)
        {
            const int maxRetries = 3;
            var retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderItems)
                        .Include(o => o.Payments)
                        .FirstOrDefaultAsync(o => o.Id == orderId);

                    if (order == null)
                        throw new DomainException("Order not found", DomainErrors.ORDER_NOT_FOUND);

                    // Validate state transition
                    if (!await ValidateOrderStateTransitionAsync(orderId, newStatus, reason))
                        throw new DomainException($"Invalid state transition to {newStatus}", DomainErrors.INVALID_ORDER_STATE);

                    // Update order status with timestamp
                    var originalStatus = order.Status;
                    order.Status = newStatus.ToString().ToUpper();
                    order.UpdatedAt = DateTime.UtcNow;

                    // Log the state change
                    _logger.LogInformation("Order {OrderId} status changed from {OldStatus} to {NewStatus}. Reason: {Reason}",
                        orderId, originalStatus, newStatus, reason ?? "System update");

                    await _context.SaveChangesAsync();
                    return order;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    retryCount++;
                    _logger.LogWarning("Concurrency conflict updating order {OrderId}, attempt {Attempt}/{MaxAttempts}",
                        orderId, retryCount, maxRetries);

                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Failed to update order {OrderId} after {MaxRetries} attempts due to concurrency conflicts",
                            orderId, maxRetries);
                        throw new DomainException("Unable to update order due to concurrent modifications", DomainErrors.CONCURRENCY_CONFLICT);
                    }

                    // Refresh the context and retry
                    foreach (var entry in _context.ChangeTracker.Entries())
                    {
                        await entry.ReloadAsync();
                    }
                }
            }

            throw new DomainException("Unexpected error in order status update", DomainErrors.SYSTEM_ERROR);
        }

        public async Task ValidateBusinessRulesAsync(Order order, OrderStatus newStatus)
        {
            switch (newStatus)
            {
                case OrderStatus.Paid:
                    // Ensure payment exists and is successful
                    var successfulPayment = await _context.Payments
                        .AnyAsync(p => p.OrderId == order.Id && p.Status == "SUCCEEDED");
                    if (!successfulPayment)
                        throw new DomainException("Cannot mark order as paid without successful payment", DomainErrors.INVALID_ORDER_STATE);
                    break;

                case OrderStatus.Processing:
                    // Ensure order is paid and items are available
                    if (order.Status != "PAID")
                        throw new DomainException("Order must be paid before processing", DomainErrors.INVALID_ORDER_STATE);
                    
                    await ValidateStockAvailabilityAsync(order.Id);
                    break;

                case OrderStatus.Shipped:
                    // Ensure shipment record exists
                    var shipmentExists = await _context.Shipments
                        .AnyAsync(s => s.OrderId == order.Id);
                    if (!shipmentExists)
                        throw new DomainException("Cannot mark order as shipped without shipment record", DomainErrors.INVALID_ORDER_STATE);
                    break;

                case OrderStatus.Delivered:
                    // Ensure shipment is in transit or delivered
                    var shipment = await _context.Shipments
                        .FirstOrDefaultAsync(s => s.OrderId == order.Id);
                    if (shipment == null || (shipment.Status != "IN_TRANSIT" && shipment.Status != "DELIVERED"))
                        throw new DomainException("Cannot mark order as delivered without valid shipment status", DomainErrors.INVALID_ORDER_STATE);
                    break;

                case OrderStatus.Cancelled:
                    // Check cancellation window and current status
                    await _orderValidationService.ValidateOrderCancellationAsync(order);
                    break;

                case OrderStatus.Refunded:
                    // Ensure order is in a refundable state
                    if (order.Status != "DELIVERED" && order.Status != "CANCELLED")
                        throw new DomainException("Order must be delivered or cancelled before refunding", DomainErrors.INVALID_ORDER_STATE);
                    break;
            }
        }

        public async Task<bool> CanCancelOrderAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return false;

                var currentStatus = Enum.Parse<OrderStatus>(order.Status, true);
                
                // Cannot cancel delivered or refunded orders
                if (currentStatus == OrderStatus.Delivered || currentStatus == OrderStatus.Refunded)
                    return false;

                // Check cancellation window
                var cancellationWindow = TimeSpan.FromHours(24);
                if (DateTime.UtcNow - order.CreatedAt > cancellationWindow && currentStatus != OrderStatus.Pending)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CanRefundOrderAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return false;

                var currentStatus = Enum.Parse<OrderStatus>(order.Status, true);
                
                // Can only refund delivered or cancelled orders
                return currentStatus == OrderStatus.Delivered || currentStatus == OrderStatus.Cancelled;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> IsOrderInTerminalStateAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null) return false;

                var currentStatus = Enum.Parse<OrderStatus>(order.Status, true);
                
                // Terminal states are Delivered, Cancelled, and Refunded
                return currentStatus == OrderStatus.Delivered || 
                       currentStatus == OrderStatus.Cancelled || 
                       currentStatus == OrderStatus.Refunded;
            }
            catch
            {
                return false;
            }
        }

        private async Task ValidateStockAvailabilityAsync(int orderId)
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.SKU)
                .Where(oi => oi.OrderId == orderId)
                .ToListAsync();

            foreach (var item in orderItems)
            {
                var availableStock = item.SKU.StockQuantity - item.SKU.ReservedQuantity;
                if (availableStock < item.Quantity)
                {
                    throw new DomainException(
                        $"Insufficient stock for item {item.SKU.SKUCode}. Available: {availableStock}, Required: {item.Quantity}",
                        DomainErrors.INSUFFICIENT_STOCK);
                }
            }
        }
    }
}