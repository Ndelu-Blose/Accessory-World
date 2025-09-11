using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.ViewModels;

namespace AccessoryWorld.Services
{
    public class PayfastService : IPayfastService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PayfastService> _logger;
        private readonly IPaymentValidationService _paymentValidationService;

        public PayfastService(IConfiguration configuration, ApplicationDbContext context, ILogger<PayfastService> logger, IPaymentValidationService paymentValidationService)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
            _paymentValidationService = paymentValidationService;
        }

        public PayfastPaymentRequest CreatePaymentRequest(Order order, string returnUrl, string cancelUrl, string notifyUrl)
        {
            var merchantId = _configuration["Payfast:MerchantId"];
            var merchantKey = _configuration["Payfast:MerchantKey"];
            var passphrase = _configuration["Payfast:Passphrase"];

            // Validate configuration
            if (string.IsNullOrEmpty(merchantId))
                throw new InvalidOperationException("Payfast MerchantId is not configured");
            if (string.IsNullOrEmpty(merchantKey))
                throw new InvalidOperationException("Payfast MerchantKey is not configured");
            if (string.IsNullOrEmpty(passphrase))
                throw new InvalidOperationException("Payfast Passphrase is not configured");

            var paymentRequest = new PayfastPaymentRequest
            {
                MerchantId = merchantId,
                MerchantKey = merchantKey,
                ReturnUrl = returnUrl,
                CancelUrl = cancelUrl,
                NotifyUrl = notifyUrl,
                NameFirst = order.User?.FirstName ?? "Customer",
                NameLast = order.User?.LastName ?? "",
                EmailAddress = order.User?.Email ?? "",
                MPaymentId = order.OrderNumber,
                Amount = order.Total.ToString("F2"),
                ItemName = $"AccessoryWorld Order #{order.OrderNumber}",
                ItemDescription = $"Order containing {order.OrderItems?.Count ?? 0} items"
            };

            // Generate signature
            paymentRequest.Signature = GenerateSignature(paymentRequest, passphrase);

            return paymentRequest;
        }

        public bool ValidateSignature(Dictionary<string, string> payfastData, string signature)
        {
            try
            {
                var passphrase = _configuration["Payfast:Passphrase"];
                if (string.IsNullOrEmpty(passphrase))
                {
                    _logger.LogError("Payfast Passphrase is not configured");
                    return false;
                }

                if (string.IsNullOrEmpty(signature))
                {
                    _logger.LogWarning("Empty signature provided for validation");
                    return false;
                }

                // Validate signature format (MD5 hash should be 32 characters)
                if (signature.Length != 32 || !System.Text.RegularExpressions.Regex.IsMatch(signature, "^[a-fA-F0-9]{32}$"))
                {
                    _logger.LogWarning($"Invalid signature format: {signature}");
                    return false;
                }
                
                var generatedSignature = GenerateSignature(payfastData, passphrase);
                var isValid = generatedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase);
                
                if (!isValid)
                {
                    _logger.LogWarning($"Signature validation failed. Expected: {generatedSignature}, Received: {signature}");
                }
                
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Payfast signature");
                return false;
            }
        }

        public async Task<bool> ProcessPaymentNotificationAsync(Dictionary<string, string> payfastData)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Get order for validation
                var orderNumber = payfastData["m_payment_id"];
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);
                
                // Use PaymentValidationService for comprehensive validation
                var validationResult = await _paymentValidationService.ValidatePaymentRequestAsync(order, "payfast");
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Payment validation failed: {Errors}", string.Join(", ", validationResult.Errors));
                    return false;
                }

                // 1. Validate required fields
                if (!ValidateRequiredFields(payfastData))
                {
                    _logger.LogWarning("Invalid Payfast notification: missing required fields");
                    return false;
                }

                // orderNumber already declared above
                var paymentStatus = payfastData["payment_status"];
                var pfPaymentId = payfastData.GetValueOrDefault("pf_payment_id", "");
                var merchantId = payfastData.GetValueOrDefault("merchant_id", "");
                var merchantKey = payfastData.GetValueOrDefault("merchant_key", "");
                
                if (!decimal.TryParse(payfastData.GetValueOrDefault("amount_gross", "0"), out var amount))
                {
                    _logger.LogWarning($"Invalid amount in payment notification: {payfastData.GetValueOrDefault("amount_gross", "0")}");
                    return false;
                }

                // 2. Validate merchant credentials
                if (!ValidateMerchantCredentials(merchantId, merchantKey))
                {
                    _logger.LogWarning($"Invalid merchant credentials in payment notification for order {orderNumber}");
                    return false;
                }

                // 3. Check for duplicate payment (idempotency)
                if (!string.IsNullOrEmpty(pfPaymentId))
                {
                    var existingPayment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.TransactionId == pfPaymentId);
                    
                    if (existingPayment != null)
                    {
                        _logger.LogInformation($"Duplicate payment notification ignored: {pfPaymentId}");
                        return true; // Return true as this is not an error
                    }
                }

                // 4. Order already retrieved above for validation
                // Load additional navigation properties if needed
                await _context.Entry(order)
                    .Collection(o => o.OrderItems)
                    .LoadAsync();
                await _context.Entry(order)
                    .Collection(o => o.Payments)
                    .LoadAsync();

                if (order == null)
                {
                    _logger.LogWarning($"Order not found for payment notification: {orderNumber}");
                    return false;
                }

                // 5. Validate payment amount matches order total
                if (!ValidatePaymentAmount(amount, order.Total))
                {
                    _logger.LogWarning($"Payment amount mismatch for order {orderNumber}. Expected: {order.Total:F2}, Received: {amount:F2}");
                    CreatePaymentExceptionRecord(order.Id, pfPaymentId, amount, "Amount mismatch");
                    await transaction.CommitAsync();
                    return false;
                }

                // 6. Validate order state transition
                if (!ValidateOrderStateTransition(order.Status, paymentStatus))
                {
                    _logger.LogWarning($"Invalid order state transition for order {orderNumber}. Current: {order.Status}, Payment: {paymentStatus}");
                    return false;
                }

                // 7. Create payment record
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Method = "PAYFAST",
                    Amount = amount,
                    Currency = "ZAR",
                    Status = MapPaymentStatusString(paymentStatus),
                    TransactionId = pfPaymentId,
                    PaymentIntentId = $"PF_{orderNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                    ProcessedAt = DateTime.UtcNow
                };

                _context.Payments.Add(payment);

                // 8. Update order status based on payment status
                UpdateOrderStatus(order, paymentStatus);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation($"Payment notification processed successfully for order {orderNumber}, transaction {pfPaymentId}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing Payfast payment notification");
                return false;
            }
        }

        private string GenerateSignature(PayfastPaymentRequest request, string passphrase)
        {
            var data = new Dictionary<string, string>
            {
                { "merchant_id", request.MerchantId },
                { "merchant_key", request.MerchantKey },
                { "return_url", request.ReturnUrl },
                { "cancel_url", request.CancelUrl },
                { "notify_url", request.NotifyUrl },
                { "name_first", request.NameFirst },
                { "name_last", request.NameLast },
                { "email_address", request.EmailAddress },
                { "m_payment_id", request.MPaymentId },
                { "amount", request.Amount },
                { "item_name", request.ItemName },
                { "item_description", request.ItemDescription }
            };

            return GenerateSignature(data, passphrase);
        }

        private string GenerateSignature(Dictionary<string, string> data, string passphrase)
        {
            // Remove signature if present
            var filteredData = data.Where(kvp => kvp.Key != "signature")
                                  .OrderBy(kvp => kvp.Key)
                                  .Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}");

            var queryString = string.Join("&", filteredData);
            if (!string.IsNullOrEmpty(passphrase))
            {
                queryString += $"&passphrase={Uri.EscapeDataString(passphrase)}";
            }

            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(queryString));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private bool ValidateRequiredFields(Dictionary<string, string> payfastData)
        {
            var requiredFields = new[] { "m_payment_id", "payment_status", "merchant_id", "merchant_key", "amount_gross" };
            return requiredFields.All(field => payfastData.ContainsKey(field) && !string.IsNullOrEmpty(payfastData[field]));
        }

        private bool ValidateMerchantCredentials(string merchantId, string merchantKey)
        {
            var configMerchantId = _configuration["Payfast:MerchantId"];
            var configMerchantKey = _configuration["Payfast:MerchantKey"];
            
            return merchantId.Equals(configMerchantId, StringComparison.OrdinalIgnoreCase) &&
                   merchantKey.Equals(configMerchantKey, StringComparison.OrdinalIgnoreCase);
        }

        private bool ValidatePaymentAmount(decimal receivedAmount, decimal expectedAmount)
        {
            // Allow for small rounding differences (1 cent)
            var tolerance = 0.01m;
            return Math.Abs(receivedAmount - expectedAmount) <= tolerance;
        }

        private bool ValidateOrderStateTransition(string currentOrderStatus, string paymentStatus)
        {
            // Define valid state transitions
            var validTransitions = new Dictionary<string, string[]>
            {
                { "PENDING", new[] { "COMPLETE", "FAILED", "CANCELLED" } },
                { "PENDING_PAYMENT", new[] { "COMPLETE", "FAILED", "CANCELLED" } },
                { "AWAITING_PAYMENT", new[] { "COMPLETE", "FAILED", "CANCELLED" } }
            };

            if (!validTransitions.ContainsKey(currentOrderStatus.ToUpper()))
            {
                return false;
            }

            return validTransitions[currentOrderStatus.ToUpper()].Contains(paymentStatus.ToUpper());
        }

        private void CreatePaymentExceptionRecord(int orderId, string transactionId, decimal amount, string reason)
        {
            var payment = new Payment
            {
                OrderId = orderId,
                Method = "PAYFAST",
                Amount = amount,
                Currency = "ZAR",
                Status = "FAILED",
                TransactionId = transactionId,
                PaymentIntentId = $"EXC_{orderId}_{DateTime.UtcNow:yyyyMMddHHmmss}",
                FailureReason = reason,
                ProcessedAt = DateTime.UtcNow
            };

            _context.Payments.Add(payment);
        }

        private void UpdateOrderStatus(Order order, string paymentStatus)
        {
            switch (paymentStatus.ToUpper())
            {
                case "COMPLETE":
                    order.Status = "PAID";
                    order.UpdatedAt = DateTime.UtcNow;

                    // Update order items status
                    if (order.OrderItems != null)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            item.Status = "CONFIRMED";
                        }
                    }
                    break;

                case "FAILED":
                case "CANCELLED":
                    order.Status = "CANCELLED";
                    order.UpdatedAt = DateTime.UtcNow;
                    break;

                case "PENDING":
                    order.Status = "PENDING_PAYMENT";
                    order.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        private string MapPayfastStatus(string payfastStatus)
        {
            return payfastStatus.ToUpper() switch
            {
                "COMPLETE" => "COMPLETED",
                "FAILED" => "FAILED",
                "CANCELLED" => "CANCELLED",
                "PENDING" => "PENDING",
                _ => "UNKNOWN"
            };
        }

        private string MapPaymentStatusString(string payfastStatus)
        {
            return payfastStatus.ToUpper() switch
            {
                "COMPLETE" => "COMPLETED",
                "FAILED" => "FAILED",
                "CANCELLED" => "CANCELLED",
                "PENDING" => "PENDING",
                _ => "UNKNOWN"
            };
        }

        public OrderStatus MapPaymentStatus(string payfastStatus)
        {
            return payfastStatus?.ToUpper() switch
            {
                "COMPLETE" => OrderStatus.Paid,
                "CANCELLED" => OrderStatus.Cancelled,
                "PENDING" => OrderStatus.Pending,
                _ => OrderStatus.Pending
            };
        }
    }
}