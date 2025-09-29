using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace AccessoryWorld.Services
{
    public class OrderService : IOrderService

    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            ApplicationDbContext context,
            ICartService cartService,
            ILogger<OrderService> logger)
        {
            _context = context;
            _cartService = cartService;
            _logger = logger;
        }

        public async Task<Order> CreateOrderAsync(string userId, Guid shippingAddressId, string fulfillmentMethod, string? notes = null, decimal creditNoteAmount = 0)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Convert PublicId to internal Id for database operations
                int? addressId = null;
                if (shippingAddressId != Guid.Empty)
                {
                    var address = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.PublicId == shippingAddressId && a.UserId == userId);
                    if (address == null)
                    {
                        throw new InvalidOperationException("Invalid shipping address");
                    }
                    addressId = address.Id;
                }
                // Get cart items
                var cart = await _cartService.GetCartAsync(userId);
                var cartItems = cart.Items;
                if (!cartItems.Any())
                {
                    throw new InvalidOperationException("Cannot create order with empty cart");
                }

                // Validate stock availability
                foreach (var item in cartItems)
                {
                    var sku = await _context.SKUs.FindAsync(item.SKUId);
                    if (sku == null || sku.StockQuantity < item.Quantity)
                    {
                        throw new InvalidOperationException($"Insufficient stock for SKU {item.SKUId}");
                    }
                }

                // Calculate totals
                var subtotal = cartItems.Sum(item => item.Quantity * item.SKU.Price);
                var taxAmount = subtotal * 0.15m; // 15% VAT
                var shippingFee = fulfillmentMethod == "DELIVERY" ? (subtotal < 500 ? 50m : 0m) : 0m;
                var totalBeforeDiscount = subtotal + taxAmount + shippingFee;
                var total = Math.Max(0, totalBeforeDiscount - creditNoteAmount);

                // Create order
                var order = new Order
                {
                    OrderNumber = await GenerateOrderNumberAsync(),
                    UserId = userId,
                    ShippingAddressId = addressId ?? 0,
                    Status = "PENDING",
                    FulfilmentMethod = fulfillmentMethod,
                    SubTotal = subtotal,
                    TaxAmount = taxAmount,
                    ShippingFee = shippingFee,
                    CreditNoteAmount = creditNoteAmount,
                    Total = total,
                    Notes = notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create order items
                foreach (var cartItem in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        SKUId = cartItem.SKUId,
                        Quantity = cartItem.Quantity,
                        UnitPrice = cartItem.SKU.Price,
                        LineTotal = cartItem.Quantity * cartItem.SKU.Price,
                        Status = "PENDING"
                    };
                    _context.OrderItems.Add(orderItem);

                    // Update stock
                    var sku = await _context.SKUs.FindAsync(cartItem.SKUId);
                    if (sku != null)
                    {
                        sku.StockQuantity -= cartItem.Quantity;
                    }
                }

                await _context.SaveChangesAsync();

                // Clear cart
                await _cartService.ClearCartAsync(userId);

                await transaction.CommitAsync();
                _logger.LogInformation($"Order {order.OrderNumber} created successfully for user {userId}");
                
                return order;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Failed to create order for user {userId}");
                throw;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SKU)
                        .ThenInclude(s => s.Product)
                .Include(o => o.ShippingAddress)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<Order?> GetOrderByNumberAsync(string orderNumber)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SKU)
                        .ThenInclude(s => s.Product)
                .Include(o => o.ShippingAddress)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SKU)
                        .ThenInclude(s => s.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            if (status == "PAID")
                order.PaidAt = DateTime.UtcNow;
            else if (status == "SHIPPED")
                order.ShippedAt = DateTime.UtcNow;
            else if (status == "DELIVERED")
                order.DeliveredAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
                
                if (order == null || order.Status == "CANCELLED") return false;

                // Restore stock
                foreach (var item in order.OrderItems)
                {
                    var sku = await _context.SKUs.FindAsync(item.SKUId);
                    if (sku != null)
                    {
                        sku.StockQuantity += item.Quantity;
                    }
                }

                order.Status = "CANCELLED";
                order.UpdatedAt = DateTime.UtcNow;
                order.Notes = (order.Notes ?? "") + $"\nCancelled: {reason}";

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public Task<decimal> CalculateOrderTotalAsync(List<CartItem> cartItems)
        {
            var subtotal = cartItems.Sum(item => item.Quantity * item.SKU.Price);
            var taxAmount = subtotal * 0.15m; // 15% VAT
            var shippingFee = subtotal < 500 ? 50m : 0m; // Free shipping over R500
            return Task.FromResult(subtotal + taxAmount + shippingFee);
        }

        public async Task<string> GenerateOrderNumberAsync()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            var orderNumber = $"AW{timestamp}{random}";
            
            // Ensure uniqueness
            while (await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
            {
                random = new Random().Next(1000, 9999);
                orderNumber = $"AW{timestamp}{random}";
            }
            
            return orderNumber;
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, decimal amount, string paymentMethod)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return false;

            var payment = new Payment
            {
                OrderId = orderId,
                Amount = amount,
                Method = paymentMethod,
                Status = "SUCCEEDED",
                TransactionId = Guid.NewGuid().ToString(),
                PaymentIntentId = $"MANUAL_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                ProcessedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
            
            // Update order status
            order.Status = "PAID";
            order.PaidAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ValidateOrderAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SKU)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return false;

            // Validate stock availability
            foreach (var item in order.OrderItems)
            {
                if (item.SKU.StockQuantity < 0)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<OrderSummaryViewModel> GetOrderSummaryAsync(int orderId)
        {
            var order = await GetOrderByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException("Order not found");

            return new OrderSummaryViewModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Status = order.Status,
                SubTotal = order.SubTotal,
                TaxAmount = order.TaxAmount,
                ShippingFee = order.ShippingFee,
                Total = order.Total,
                CreatedAt = order.CreatedAt,
                OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
                {
                    ProductName = oi.SKU.Product.Name,
                    SKUName = oi.SKU.Variant ?? oi.SKU.SKUCode,
                    ProductImage = oi.SKU.Product.ProductImages.FirstOrDefault()?.ImageUrl,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    LineTotal = oi.LineTotal
                }).ToList()
            };
        }
    }
}