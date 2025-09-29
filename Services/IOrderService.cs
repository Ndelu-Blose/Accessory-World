using AccessoryWorld.Models;
using AccessoryWorld.ViewModels;

namespace AccessoryWorld.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(string userId, Guid shippingAddressId, string fulfillmentMethod, string? notes = null, decimal creditNoteAmount = 0);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<Order?> GetOrderByNumberAsync(string orderNumber);
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task<bool> UpdateOrderStatusAsync(int orderId, string status);
        Task<bool> CancelOrderAsync(int orderId, string reason);
        Task<decimal> CalculateOrderTotalAsync(List<CartItem> cartItems);
        Task<string> GenerateOrderNumberAsync();
        Task<bool> ProcessPaymentAsync(int orderId, decimal amount, string paymentMethod);
        Task<bool> ValidateOrderAsync(int orderId);
        Task<OrderSummaryViewModel> GetOrderSummaryAsync(int orderId);
    }
}