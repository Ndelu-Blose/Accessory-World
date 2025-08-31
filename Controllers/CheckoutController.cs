using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Services;
using AccessoryWorld.ViewModels;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using AccessoryWorld.Security;

namespace AccessoryWorld.Controllers
{
    [Authorize]
    [ValidateAntiForgeryToken]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CartService _cartService;
        private readonly IPayfastService _payfastService;
        private readonly ISecurityValidationService _securityValidation;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            CartService cartService,
            IPayfastService payfastService,
            ISecurityValidationService securityValidation,
            ILogger<CheckoutController> logger)
        {
            _context = context;
            _userManager = userManager;
            _cartService = cartService;
            _payfastService = payfastService;
            _securityValidation = securityValidation;
            _logger = logger;
        }

        // GET: /Checkout
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Get cart items
            var cart = await _cartService.GetCartAsync(HttpContext.Session.Id, userId);
            if (!cart.Items.Any())
            {
                TempData["Error"] = "Your cart is empty. Please add items before checkout.";
                return RedirectToAction("Index", "Cart");
            }
            var cartItems = cart.Items;

            // Get user addresses
            var user = await _userManager.Users
                .Include(u => u.Addresses)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var viewModel = new CheckoutViewModel
            {
                CartItems = cartItems,
                UserAddresses = user?.Addresses.ToList() ?? new List<Address>(),
                SubTotal = cartItems.Sum(item => item.UnitPrice * item.Quantity),
                VATAmount = 0, // Calculate based on business rules
                ShippingFee = 0, // Will be calculated based on delivery method
                Total = cartItems.Sum(item => item.UnitPrice * item.Quantity)
            };

            // Calculate tax (15% VAT for South Africa)
            viewModel.VATAmount = Math.Round(viewModel.SubTotal * 0.15m, 2);
            viewModel.Total = viewModel.SubTotal + viewModel.VATAmount + viewModel.ShippingFee;

            return View("~/Views/Customer/Checkout/Index.cshtml", viewModel);
        }

        // POST: /Checkout/CalculateShipping
        [HttpPost]
        public async Task<IActionResult> CalculateShipping([FromBody] ShippingCalculationRequest request)
        {
            try
            {
                decimal shippingFee = 0;

                if (request.FulfillmentMethod == "delivery")
                {
                    // Simple shipping calculation - in real app, integrate with courier APIs
                    var address = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.Id == request.AddressId);

                    if (address != null)
                    {
                        // Basic shipping rates by province
                        shippingFee = (address.Province?.ToUpper() ?? "OTHER") switch
                        {
                            "GAUTENG" => 99.00m,
                            "WESTERN CAPE" => 120.00m,
                            "KWAZULU-NATAL" => 110.00m,
                            "EASTERN CAPE" => 130.00m,
                            "FREE STATE" => 115.00m,
                            "LIMPOPO" => 125.00m,
                            "MPUMALANGA" => 115.00m,
                            "NORTH WEST" => 120.00m,
                            "NORTHERN CAPE" => 140.00m,
                            _ => 150.00m // Default for unknown provinces
                        };
                    }
                }
                // PICKUP has no shipping fee

                return Json(new { success = true, shippingFee });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: /Checkout/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return RedirectToAction("Login", "Account");

                // Get cart items
                var cart = await _cartService.GetCartAsync(HttpContext.Session.Id, userId);
                if (!cart.Items.Any())
                {
                    TempData["Error"] = "Your cart is empty.";
                    return RedirectToAction("Index", "Cart");
                }
                var cartItems = cart.Items;

                // Validate address for delivery
                if (model.FulfillmentMethod == "delivery" && model.SelectedAddressId == null)
                {
                    TempData["Error"] = "Please select a delivery address.";
                    return RedirectToAction("Index");
                }

                // Create order
                var order = new Order
                {
                    OrderNumber = GenerateOrderNumber(),
                    UserId = userId,
                    ShippingAddressId = model.SelectedAddressId ?? 0,
                    Status = "PENDING",
                    FulfilmentMethod = model.FulfillmentMethod.ToUpper(),
                    SubTotal = model.SubTotal,
                    TaxAmount = model.VATAmount,
                    ShippingFee = model.ShippingFee,
                    DiscountAmount = 0,
                    CreditNoteAmount = 0,
                    Total = model.Total,
                    Currency = "ZAR",
                    Notes = model.Notes,
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
                        UnitPrice = cartItem.UnitPrice,
                        LineTotal = cartItem.UnitPrice * cartItem.Quantity,
                        Status = "PENDING"
                    };
                    _context.OrderItems.Add(orderItem);
                }

                await _context.SaveChangesAsync();

                // Clear cart
                await _cartService.ClearCartAsync(userId, HttpContext.Session.Id);

                // Redirect to payment
                return RedirectToAction("Payment", new { orderId = order.Id });
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while placing your order. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Checkout/Payment/{orderId}
        public async Task<IActionResult> Payment(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SKU)
                        .ThenInclude(s => s.Product)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new PaymentViewModel
            {
                Order = order
            };

            return View("~/Views/Customer/Checkout/Payment.cshtml", viewModel);
        }

        // POST: /Checkout/ProcessPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int orderId, string paymentMethod)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.ShippingAddress)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            if (paymentMethod == "payfast")
            {
                var returnUrl = Url.Action("PaymentReturn", "Checkout", new { orderId }, Request.Scheme);
                var cancelUrl = Url.Action("PaymentCancel", "Checkout", new { orderId }, Request.Scheme);
                var notifyUrl = Url.Action("PaymentNotify", "Checkout", null, Request.Scheme);

                var paymentRequest = _payfastService.CreatePaymentRequest(order, returnUrl!, cancelUrl!, notifyUrl!);
                
                return View("~/Views/Customer/Checkout/PayfastRedirect.cshtml", paymentRequest);
            }
            else if (paymentMethod == "eft")
            {
                // Handle EFT payment - mark as pending
                var payment = new Payment
                {
                    OrderId = order.Id,
                    Method = "EFT",
                    Amount = order.Total,
                    Currency = "ZAR",
                    Status = "PENDING",
                    PaymentIntentId = $"EFT_{order.OrderNumber}_{DateTime.UtcNow:yyyyMMddHHmmss}"
                };

                _context.Payments.Add(payment);
                order.Status = "PENDING_PAYMENT";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                TempData["Success"] = "Your order has been placed. Please make the EFT payment using the details provided.";
                return RedirectToAction("OrderConfirmation", new { orderId });
            }

            TempData["Error"] = "Invalid payment method selected.";
            return RedirectToAction("Payment", new { orderId });
        }

        // GET: /Checkout/PaymentReturn
        public async Task<IActionResult> PaymentReturn(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            TempData["Success"] = "Payment completed successfully! Your order is being processed.";
            return RedirectToAction("OrderConfirmation", new { orderId });
        }

        // GET: /Checkout/PaymentCancel
        public async Task<IActionResult> PaymentCancel(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            TempData["Warning"] = "Payment was cancelled. You can try again or choose a different payment method.";
            return RedirectToAction("Payment", new { orderId });
        }

        // POST: /Checkout/PaymentNotify
        [HttpPost]
        [IgnoreAntiforgeryToken] // Payfast webhooks don't include anti-forgery tokens
        public async Task<IActionResult> PaymentNotify()
        {
            try
            {
                // 1. Validate request origin (optional - can be configured to only accept from Payfast IPs)
                var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
                _logger.LogInformation($"Payment notification received from IP: {clientIp}");

                // 2. Validate content type
                if (!Request.HasFormContentType)
                {
                    _logger.LogWarning("Payment notification received with invalid content type");
                    return BadRequest("Invalid content type");
                }

                var payfastData = new Dictionary<string, string>();
                
                // 3. Read and validate form data
                foreach (var key in Request.Form.Keys)
                {
                    var value = Request.Form[key].ToString();
                    if (string.IsNullOrEmpty(value))
                    {
                        _logger.LogWarning($"Empty value received for key: {key}");
                        continue;
                    }
                    payfastData[key] = value;
                }

                // 4. Log received data (excluding sensitive information)
                var logData = payfastData.Where(kvp => !IsSensitiveField(kvp.Key))
                                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                _logger.LogInformation($"Payment notification data: {string.Join(", ", logData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

                // 5. Validate signature first
                var signature = payfastData.GetValueOrDefault("signature", "");
                if (!_payfastService.ValidateSignature(payfastData, signature))
                {
                    _logger.LogWarning("Payment notification rejected: Invalid signature");
                    return BadRequest("Invalid signature");
                }

                // 6. Process payment notification with comprehensive validation
                var success = await _payfastService.ProcessPaymentNotificationAsync(payfastData);
                
                if (success)
                {
                    _logger.LogInformation($"Payment notification processed successfully for order {payfastData.GetValueOrDefault("m_payment_id", "unknown")}");
                    return Ok("Payment notification processed successfully");
                }
                else
                {
                    _logger.LogWarning($"Payment notification processing failed for order {payfastData.GetValueOrDefault("m_payment_id", "unknown")}");
                    return BadRequest("Processing failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment notification");
                return BadRequest("Error processing notification");
            }
        }

        private bool IsSensitiveField(string fieldName)
        {
            var sensitiveFields = new[] { "merchant_key", "signature", "passphrase" };
            return sensitiveFields.Contains(fieldName.ToLower());
        }

        // GET: /Checkout/OrderConfirmation
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SKU)
                        .ThenInclude(s => s.Product)
                .Include(o => o.ShippingAddress)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/Customer/Checkout/OrderConfirmation.cshtml", order);
        }

        // GET: /Checkout/AddAddress
        public IActionResult AddAddress()
        {
            return View("~/Views/Customer/Checkout/AddAddress.cshtml", new Address());
        }

        // POST: /Checkout/AddAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address address)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                address.UserId = userId!;
                address.CreatedAt = DateTime.UtcNow;

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Address added successfully.";
                return RedirectToAction("Index");
            }

            return View("~/Views/Customer/Checkout/AddAddress.cshtml", address);
        }

        private string GenerateOrderNumber()
        {
            return $"ORD{DateTime.UtcNow:yyyyMMdd}{DateTime.UtcNow.Ticks.ToString().Substring(10)}";
        }
    }
}