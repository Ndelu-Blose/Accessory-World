using AccessoryWorld.ViewModels;
using AccessoryWorld.Models;

namespace AccessoryWorld.Services
{
    public interface IPayfastService
    {
        /// <summary>
        /// Creates a Payfast payment request with all necessary parameters
        /// </summary>
        /// <param name="order">The order to create payment for</param>
        /// <param name="returnUrl">URL to redirect after successful payment</param>
        /// <param name="cancelUrl">URL to redirect after cancelled payment</param>
        /// <param name="notifyUrl">URL for Payfast to send payment notifications</param>
        /// <returns>PayfastPaymentRequest with all required fields</returns>
        PayfastPaymentRequest CreatePaymentRequest(Order order, string returnUrl, string cancelUrl, string notifyUrl);
        
        /// <summary>
        /// Validates the signature from Payfast to ensure authenticity
        /// </summary>
        /// <param name="parameters">Payment parameters from Payfast</param>
        /// <param name="signature">Signature to validate</param>
        /// <returns>True if signature is valid</returns>
        bool ValidateSignature(Dictionary<string, string> parameters, string signature);
        
        /// <summary>
        /// Processes payment notification from Payfast (IPN)
        /// </summary>
        /// <param name="parameters">Payment notification parameters</param>
        /// <returns>True if notification was processed successfully</returns>
        Task<bool> ProcessPaymentNotificationAsync(Dictionary<string, string> parameters);
        
        /// <summary>
        /// Maps Payfast payment status to internal order status
        /// </summary>
        /// <param name="payfastStatus">Status from Payfast</param>
        /// <returns>Corresponding OrderStatus</returns>
        OrderStatus MapPaymentStatus(string payfastStatus);
    }
}