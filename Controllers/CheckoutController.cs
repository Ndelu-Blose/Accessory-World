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
    public class ValidateCreditNoteRequest
    {
        public string CreditNoteCode { get; set; } = string.Empty;
        public decimal RequestedAmount { get; set; }
    }

    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICartService _cartService;
        private readonly IOrderService _orderService;
        private readonly IPayfastService _payfastService;
        private readonly ISecurityValidationService _securityValidation;
        private readonly ILogger<CheckoutController> _logger;
        private readonly IAddressService _addressService;
        private readonly ICheckoutService _checkoutService;
        private readonly ICreditNoteService _creditNoteService;

        public CheckoutController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICartService cartService,
            IOrderService orderService,
            IPayfastService payfastService,
            ISecurityValidationService securityValidation,
            ILogger<CheckoutController> logger,
            IAddressService addressService,
            ICheckoutService checkoutService,
            ICreditNoteService creditNoteService)
        {
            _context = context;
            _userManager = userManager;
            _cartService = cartService;
            _orderService = orderService;
            _payfastService = payfastService;
            _securityValidation = securityValidation;
            _logger = logger;
            _addressService = addressService;
            _checkoutService = checkoutService;
            _creditNoteService = creditNoteService;
        }

        // GET: /Checkout
        public async Task<IActionResult> Index(string? creditNoteCode = null, decimal? creditNoteAmount = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Get cart items
            var cart = await _cartService.GetCartAsync(HttpContext.Session.Id, userId);
            var cartItems = cart?.Items ?? new List<CartItem>();
            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty. Please add items before checkout.";
                return RedirectToAction("Index", "Cart");
            }

            // Get user addresses using AddressService to ensure proper scoping
            var userAddresses = await _addressService.GetUserAddressesAsync(userId);

            // Get user's credit notes
            var userCreditNotes = await _creditNoteService.GetUserCreditNotesAsync(userId, true);
            var availableCreditBalance = await _creditNoteService.GetUserCreditBalanceAsync(userId);

            _logger.LogInformation($"Debug: User ID: {userId}");
            _logger.LogInformation($"Debug: Address count: {userAddresses.Count}");

            // Check if credit note is applied
            bool hasValidCreditNote = false;
            decimal appliedCreditAmount = 0;
            
            if (!string.IsNullOrEmpty(creditNoteCode) && creditNoteAmount.HasValue)
            {
                var isValid = await _checkoutService.ValidateCreditNoteForCheckoutAsync(
                    creditNoteCode, creditNoteAmount.Value, userId);
                
                if (isValid)
                {
                    hasValidCreditNote = true;
                    appliedCreditAmount = creditNoteAmount.Value;
                }
            }

            var viewModel = new CheckoutViewModel
            {
                CartItems = cartItems.Select(item => new CartItemViewModel
                {
                    Id = item.Id,
                    ProductName = item.Product.Name,
                    SKUName = item.SKU.Name ?? "Standard",
                    Quantity = item.Quantity,
                    Price = item.UnitPrice,
                    LineTotal = item.TotalPrice,
                    ImageUrl = item.Product.ProductImages.FirstOrDefault()?.ImageUrl
                }).ToList(),
                UserAddresses = userAddresses,
                UserCreditNotes = userCreditNotes.Select(cn => new CreditNoteViewModel(cn)).ToList(),
                AvailableCreditBalance = availableCreditBalance,
                SubTotal = cartItems.Sum(item => item.TotalPrice),
                TaxAmount = 0, // Will be calculated
                ShippingFee = 0, // Will be calculated based on delivery method
                DiscountAmount = 0,
                CreditNoteAmount = appliedCreditAmount,
                CreditNoteCode = creditNoteCode,
                CreditNoteRequestedAmount = creditNoteAmount,
                HasValidCreditNote = hasValidCreditNote,
                Total = 0 // Will be calculated
            };

            // Calculate tax (15% VAT for South Africa)
            viewModel.TaxAmount = Math.Round(viewModel.SubTotal * 0.15m, 2);
            
            // Calculate total including credit note discount
            var totalBeforeDiscount = viewModel.SubTotal + viewModel.TaxAmount + viewModel.ShippingFee;
            viewModel.Total = Math.Max(0, totalBeforeDiscount - viewModel.CreditNoteAmount);

            return View("~/Views/Customer/Checkout/Index.cshtml", viewModel);
        }

        // POST: /Checkout/ProcessOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessOrder(CheckoutViewModel model)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                    return RedirectToAction("Login", "Account");

                if (!ModelState.IsValid)
                {
                    // Reload the view with validation errors using AddressService
                    var cart = await _cartService.GetCartAsync(HttpContext.Session.Id, userId);
                    var cartItems = cart?.Items ?? new List<CartItem>();
                    var userAddresses = await _addressService.GetUserAddressesAsync(userId);

                    model.CartItems = cartItems.Select(item => new CartItemViewModel
                    {
                        Id = item.Id,
                        ProductName = item.SKU.Product.Name,
                        SKUName = item.SKU.Name,
                        Quantity = item.Quantity,
                        Price = item.SKU.Price,
                        LineTotal = item.Quantity * item.SKU.Price,
                        ImageUrl = item.SKU.Product.ProductImages.FirstOrDefault()?.ImageUrl
                    }).ToList();

                    model.UserAddresses = userAddresses;

                    return View("~/Views/Customer/Checkout/Index.cshtml", model);
                }

                // Create the order
                // For pickup orders, shipping address is not required
                var shippingAddressId = model.FulfillmentMethod == "PICKUP" ? Guid.Empty : (model.ShippingAddressId ?? model.SelectedAddressId ?? Guid.Empty);
                
                // Handle inline address form (workaround for address bug)
                if (model.FulfillmentMethod == "DELIVERY" && model.UseInlineAddress && model.InlineAddress != null)
                {
                    // Validate inline address
                    if (string.IsNullOrWhiteSpace(model.InlineAddress.FullName) ||
                        string.IsNullOrWhiteSpace(model.InlineAddress.AddressLine1) ||
                        string.IsNullOrWhiteSpace(model.InlineAddress.City) ||
                        string.IsNullOrWhiteSpace(model.InlineAddress.Province) ||
                        string.IsNullOrWhiteSpace(model.InlineAddress.PostalCode) ||
                        string.IsNullOrWhiteSpace(model.InlineAddress.PhoneNumber))
                    {
                        ModelState.AddModelError("", "Please fill in all required address fields.");
                        
                        // Reload the view with validation errors
                        var cart = await _cartService.GetCartAsync(HttpContext.Session.Id, userId);
                        var cartItems = cart?.Items ?? new List<CartItem>();
                        var userAddresses = await _addressService.GetUserAddressesAsync(userId);

                        model.CartItems = cartItems.Select(item => new CartItemViewModel
                        {
                            Id = item.Id,
                            ProductName = item.SKU.Product.Name,
                            SKUName = item.SKU.Name,
                            Quantity = item.Quantity,
                            Price = item.SKU.Price,
                            LineTotal = item.Quantity * item.SKU.Price,
                            ImageUrl = item.SKU.Product.ProductImages.FirstOrDefault()?.ImageUrl
                        }).ToList();

                        model.UserAddresses = userAddresses;
                        return View("~/Views/Customer/Checkout/Index.cshtml", model);
                    }

                    // Try to save the inline address
                    try
                    {
                        var addressId = await _addressService.AddAddressAsync(model.InlineAddress, userId);
                        if (addressId != Guid.Empty)
                        {
                            shippingAddressId = addressId;
                            _logger.LogInformation("Successfully created address from inline form: {AddressId}", addressId);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create address from inline form, using temporary address for order");
                            // Continue with order creation using inline address data directly
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating address from inline form, using temporary address for order");
                        // Continue with order creation using inline address data directly
                    }
                }
                
                // Validate address for delivery orders only
                if (model.FulfillmentMethod == "DELIVERY" && shippingAddressId == Guid.Empty && !model.UseInlineAddress)
                {
                    ModelState.AddModelError("", "Please select a delivery address.");
                    
                    // Reload the view with validation errors using AddressService
                    var cart = await _cartService.GetCartAsync(HttpContext.Session.Id, userId);
                    var cartItems = cart?.Items ?? new List<CartItem>();
                    var userAddresses = await _addressService.GetUserAddressesAsync(userId);

                    model.CartItems = cartItems.Select(item => new CartItemViewModel
                    {
                        Id = item.Id,
                        ProductName = item.SKU.Product.Name,
                        SKUName = item.SKU.Name,
                        Quantity = item.Quantity,
                        Price = item.SKU.Price,
                        LineTotal = item.Quantity * item.SKU.Price,
                        ImageUrl = item.SKU.Product.ProductImages.FirstOrDefault()?.ImageUrl
                    }).ToList();

                    model.UserAddresses = userAddresses;

                    return View("~/Views/Customer/Checkout/Index.cshtml", model);
                }

                // Validate that the selected address belongs to the current user
                if (model.FulfillmentMethod == "DELIVERY" && shippingAddressId != Guid.Empty)
                {
                    var selectedAddress = await _addressService.GetAddressByPublicIdAsync(shippingAddressId, userId);
                    if (selectedAddress == null)
                    {
                        ModelState.AddModelError("", "Invalid delivery address selected.");
                        
                        // Reload the view with validation errors using AddressService
                        var cart = await _cartService.GetCartAsync(HttpContext.Session.Id, userId);
                        var cartItems = cart?.Items ?? new List<CartItem>();
                        var userAddresses = await _addressService.GetUserAddressesAsync(userId);

                        model.CartItems = cartItems.Select(item => new CartItemViewModel
                        {
                            Id = item.Id,
                            ProductName = item.SKU.Product.Name,
                            SKUName = item.SKU.Name,
                            Quantity = item.Quantity,
                            Price = item.SKU.Price,
                            LineTotal = item.Quantity * item.SKU.Price,
                            ImageUrl = item.SKU.Product.ProductImages.FirstOrDefault()?.ImageUrl
                        }).ToList();

                        model.UserAddresses = userAddresses;

                        return View("~/Views/Customer/Checkout/Index.cshtml", model);
                    }
                }
                
                // Create checkout session and process order with credit notes
                var checkoutSession = await _checkoutService.CreateCheckoutSessionAsync(userId, model.CreditNoteCode, model.CreditNoteRequestedAmount);
                
                var order = await _checkoutService.ProcessOrderAsync(
                    checkoutSession.SessionId,
                    userId, 
                    shippingAddressId, 
                    model.FulfillmentMethod, 
                    model.Notes);

                TempData["Success"] = $"Order {order.OrderNumber} created successfully! Please complete your payment.";
                return RedirectToAction("Payment", new { orderId = order.Id });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order for user {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
                TempData["Error"] = "An error occurred while processing your order. Please try again.";
                return RedirectToAction("Index");
            }
        }

        // GET: /Checkout/OrderConfirmation/{orderId}
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null || order.UserId != userId)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("Index", "Home");
            }

            return View("~/Views/Customer/Checkout/OrderConfirmation.cshtml", order);
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
                        .FirstOrDefaultAsync(a => a.PublicId == request.AddressId);

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
                // Convert PublicId to internal Id for database operations
                int addressId = 0;
                if (model.SelectedAddressId.HasValue && model.SelectedAddressId != Guid.Empty)
                {
                    var address = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.PublicId == model.SelectedAddressId && a.UserId == userId);
                    if (address != null)
                    {
                        addressId = address.Id;
                    }
                }

                var order = new Order
                {
                    OrderNumber = GenerateOrderNumber(),
                    UserId = userId,
                    ShippingAddressId = addressId,
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



        // GET: /Checkout/AddAddress
        public async Task<IActionResult> AddAddress()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            var model = await _addressService.CreateAddressViewModelForUserAsync(userId);
            return View("~/Views/Customer/Checkout/AddAddress.cshtml", model);
        }

        // POST: /Checkout/AddAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(AddressViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid)
            {
                // Log all model errors to help diagnose future 400s
                var errors = ModelState
                    .Where(kv => kv.Value?.Errors.Count > 0)
                    .Select(kv => new { Field = kv.Key, Messages = kv.Value!.Errors.Select(e => e.ErrorMessage) });
                _logger.LogWarning("Checkout address form invalid: {@Errors}", errors);

                return View("~/Views/Customer/Checkout/AddAddress.cshtml", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            try
            {
                await _addressService.AddAddressAsync(model, userId);
                TempData["Success"] = "Address added successfully.";
                
                // Handle return URL for checkout flow
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving address for user {UserId}", userId);
                ModelState.AddModelError("", "An error occurred while saving the address. Please try again.");
                return View("~/Views/Customer/Checkout/AddAddress.cshtml", model);
            }
        }

        // POST: /Checkout/ValidateCreditNote
        [HttpPost]
        public async Task<IActionResult> ValidateCreditNote([FromBody] ValidateCreditNoteRequest request)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Json(new { success = false, message = "User not authenticated" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }
                
                // Validate credit note
                var isValid = await _checkoutService.ValidateCreditNoteForCheckoutAsync(
                    request.CreditNoteCode, request.RequestedAmount, userId);

                if (!isValid)
                {
                    return Json(new { success = false, message = "Invalid credit note or insufficient balance" });
                }

                // Try to lock the credit note for the current session
                var sessionId = HttpContext.Session.GetString("CheckoutSessionId");
                if (!string.IsNullOrEmpty(sessionId) && Guid.TryParse(sessionId, out var parsedSessionId))
                {
                    var lockSuccess = await _checkoutService.LockCreditNoteAsync(
                        request.CreditNoteCode, request.RequestedAmount, parsedSessionId);
                    
                    if (!lockSuccess)
                    {
                        return Json(new { success = false, message = "Credit note is currently being used by another session" });
                    }
                }

                return Json(new { 
                    success = true, 
                    message = "Credit note validated successfully",
                    amount = request.RequestedAmount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating credit note {CreditNoteCode}", request.CreditNoteCode);
                return Json(new { success = false, message = "An error occurred while validating the credit note" });
            }
        }

        private string GenerateOrderNumber()
        {
            return $"ORD{DateTime.UtcNow:yyyyMMdd}{DateTime.UtcNow.Ticks.ToString().Substring(10)}";
        }
    }
}