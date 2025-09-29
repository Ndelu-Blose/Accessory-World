using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace AccessoryWorld.Services
{
    public class OrderValidationService
    {
        private readonly ApplicationDbContext _context;
        
        public OrderValidationService(ApplicationDbContext context)
        {
            _context = context;
        }
        
        private OrderStatus ParseOrderStatus(string status)
        {
            return status?.ToUpper() switch
            {
                "PENDING" => OrderStatus.Pending,
                "PAID" => OrderStatus.Paid,
                "PROCESSING" => OrderStatus.Processing,
                "SHIPPED" => OrderStatus.Shipped,
                "DELIVERED" => OrderStatus.Delivered,
                "CANCELLED" => OrderStatus.Cancelled,
                "REFUNDED" => OrderStatus.Refunded,
                _ => throw new DomainException($"Invalid order status: {status}", DomainErrors.INVALID_ORDER_STATE)
            };
        }
        
        private string OrderStatusToString(OrderStatus status)
        {
            return status.ToString().ToUpper();
        }
        
        public Task ValidateOrderTransitionAsync(Order order, OrderStatus newStatus)
        {
            if (order == null)
                throw new DomainException("Order not found", DomainErrors.ORDER_NOT_FOUND);
            
            var currentStatus = ParseOrderStatus(order.Status);
            
            // Define valid state transitions
            var validTransitions = new Dictionary<OrderStatus, List<OrderStatus>>
            {
                { OrderStatus.Pending, new List<OrderStatus> { OrderStatus.Paid, OrderStatus.Processing, OrderStatus.Cancelled } },
                { OrderStatus.Paid, new List<OrderStatus> { OrderStatus.Processing, OrderStatus.Cancelled } },
                { OrderStatus.Processing, new List<OrderStatus> { OrderStatus.Shipped, OrderStatus.Delivered, OrderStatus.Cancelled } },
                { OrderStatus.Shipped, new List<OrderStatus> { OrderStatus.Delivered } },
                { OrderStatus.Delivered, new List<OrderStatus> { OrderStatus.Refunded } },
                { OrderStatus.Cancelled, new List<OrderStatus> { OrderStatus.Refunded } },
                { OrderStatus.Refunded, new List<OrderStatus>() } // Terminal state
            };
            
            if (!validTransitions.ContainsKey(currentStatus))
                throw new DomainException($"Invalid current order status: {order.Status}", DomainErrors.INVALID_ORDER_STATE);
            
            if (!validTransitions[currentStatus].Contains(newStatus))
                throw new DomainException($"Invalid status transition from {order.Status} to {OrderStatusToString(newStatus)}", DomainErrors.INVALID_ORDER_STATE);
            
            return Task.CompletedTask;
        }
        
        public Task ValidateOrderCancellationAsync(Order order)
        {
            if (order == null)
                throw new DomainException("Order not found", DomainErrors.ORDER_NOT_FOUND);
            
            var currentStatus = ParseOrderStatus(order.Status);
            
            // Cannot cancel delivered or refunded orders
            if (currentStatus == OrderStatus.Delivered || currentStatus == OrderStatus.Refunded)
                throw new DomainException("Cannot cancel a delivered or refunded order", DomainErrors.INVALID_ORDER_STATE);
            
            // Check if order was placed within cancellation window (e.g., 24 hours)
            var cancellationWindow = TimeSpan.FromHours(24);
            if (DateTime.UtcNow - order.CreatedAt > cancellationWindow && currentStatus != OrderStatus.Pending)
                throw new DomainException("Order cancellation window has expired", DomainErrors.INVALID_ORDER_STATE);
            
            return Task.CompletedTask;
        }
        
        public async Task ValidateOrderItemsAsync(List<CartItem> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
                throw new DomainException("Order must contain at least one item", DomainErrors.INVALID_QUANTITY);
            
            foreach (var item in cartItems)
            {
                // Validate product availability
                var sku = await _context.SKUs
                    .Include(s => s.Product)
                    .FirstOrDefaultAsync(s => s.Id == item.SKUId);
                
                if (sku?.Product?.IsActive != true)
                    throw new DomainException($"Product {sku?.Product?.Name ?? "Unknown"} is no longer available", DomainErrors.PRODUCT_INACTIVE);
                
                // Validate stock availability
                var availableStock = sku.StockQuantity - sku.ReservedQuantity;
                if (availableStock < item.Quantity)
                    throw new DomainException($"Insufficient stock for {sku.Product.Name}. Available: {availableStock}, Requested: {item.Quantity}", DomainErrors.INSUFFICIENT_STOCK);
                
                // Validate quantity limits
                if (item.Quantity <= 0 || item.Quantity > 100)
                    throw new DomainException($"Invalid quantity for {sku.Product.Name}. Must be between 1 and 100", DomainErrors.INVALID_QUANTITY);
            }
        }
        
        public void ValidateOrderAmounts(decimal subtotal, decimal vatAmount, decimal shippingFee, decimal total)
        {
            if (subtotal < 0)
                throw new DomainException("Subtotal cannot be negative", DomainErrors.INVALID_PAYMENT_AMOUNT);
            
            if (vatAmount < 0)
                throw new DomainException("VAT amount cannot be negative", DomainErrors.INVALID_PAYMENT_AMOUNT);
            
            if (shippingFee < 0)
                throw new DomainException("Shipping fee cannot be negative", DomainErrors.INVALID_PAYMENT_AMOUNT);
            
            if (total <= 0)
                throw new DomainException("Total amount must be greater than zero", DomainErrors.INVALID_PAYMENT_AMOUNT);
            
            // Validate calculation accuracy (allowing for small rounding differences)
            var calculatedTotal = subtotal + vatAmount + shippingFee;
            if (Math.Abs(calculatedTotal - total) > 0.01m)
                throw new DomainException($"Total amount calculation error. Expected: {calculatedTotal:C}, Actual: {total:C}", DomainErrors.INVALID_PAYMENT_AMOUNT);
        }
        
        public async Task ValidatePaymentAmountAsync(int orderId, decimal paymentAmount)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new DomainException("Order not found", DomainErrors.ORDER_NOT_FOUND);
            
            if (Math.Abs(order.Total - paymentAmount) > 0.01m)
                throw new DomainException($"Payment amount mismatch. Order total: {order.Total:C}, Payment amount: {paymentAmount:C}", DomainErrors.INVALID_PAYMENT_AMOUNT);
        }
    }
}