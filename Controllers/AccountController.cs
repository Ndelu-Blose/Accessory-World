using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Services;
using AccessoryWorld.ViewModels;
using System.Security.Claims;

namespace AccessoryWorld.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;
        private readonly IWishlistService _wishlistService;
        private readonly IRecommendationService _recommendationService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<AccountController> logger,
            IWishlistService wishlistService,
            IRecommendationService recommendationService)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
            _wishlistService = wishlistService;
            _recommendationService = recommendationService;
        }

        // GET: Account/Profile
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty
            };

            return View("~/Views/Customer/Account/Profile.cshtml", viewModel);
        }

        // POST: Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Customer/Account/Profile.cshtml", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View("~/Views/Customer/Account/AddAddress.cshtml", model);
        }

        // GET: Account/Addresses
        public async Task<IActionResult> Addresses()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            var addresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.IsDefault)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View("~/Views/Customer/Account/Addresses.cshtml", addresses);
        }

        // GET: Account/AddAddress
        public IActionResult AddAddress()
        {
            return View("~/Views/Customer/Account/AddAddress.cshtml", new AddressViewModel());
        }

        // POST: Account/AddAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(AddressViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Customer/Account/EditAddress.cshtml", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            // If this is set as default, remove default from other addresses
            if (model.IsDefault)
            {
                var existingAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault)
                    .ToListAsync();
                
                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                }
            }

            var address = new Address
            {
                UserId = userId,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                City = model.City,
                Province = model.Province,
                PostalCode = model.PostalCode,
                Country = model.Country,
                IsDefault = model.IsDefault
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Address added successfully!";
            return RedirectToAction(nameof(Addresses));
        }

        // GET: Account/EditAddress/5
        public async Task<IActionResult> EditAddress(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            var viewModel = new AddressViewModel
            {
                Id = address.Id,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                Province = address.Province,
                PostalCode = address.PostalCode,
                Country = address.Country,
                IsDefault = address.IsDefault
            };

            return View("~/Views/Customer/Account/EditAddress.cshtml", viewModel);
        }

        // POST: Account/EditAddress/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(int id, AddressViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            // If this is set as default, remove default from other addresses
            if (model.IsDefault && !address.IsDefault)
            {
                var existingAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault && a.Id != id)
                    .ToListAsync();
                
                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                }
            }

            address.AddressLine1 = model.AddressLine1;
            address.AddressLine2 = model.AddressLine2;
            address.City = model.City;
            address.Province = model.Province;
            address.PostalCode = model.PostalCode;
            address.Country = model.Country;
            address.IsDefault = model.IsDefault;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Address updated successfully!";
            return RedirectToAction(nameof(Addresses));
        }

        // POST: Account/DeleteAddress/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return NotFound();
            }

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

            if (address == null)
            {
                return NotFound();
            }

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Address deleted successfully!";
            return RedirectToAction(nameof(Addresses));
        }

        // GET: Account/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Get comprehensive order statistics
            var orders = await _context.Orders
                .Where(o => o.UserId == userId)
                .ToListAsync();

            var totalOrders = orders.Count;
            var pendingOrders = orders.Count(o => o.Status == "Pending" || o.Status == "Processing");
            var completedOrders = orders.Count(o => o.Status == "Delivered" || o.Status == "Completed");
            var totalSpent = orders.Where(o => o.Status != "Cancelled").Sum(o => o.Total);
            var lastOrderDate = orders.OrderByDescending(o => o.CreatedAt).FirstOrDefault()?.CreatedAt;

            // Get recent orders (last 5)
            var recentOrders = orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new RecentOrderSummary
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    Status = o.Status,
                    Total = o.Total,
                    Currency = o.Currency,
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.OrderItems?.Count ?? 0,
                    TrackingNumber = o.Shipment?.TrackingNumber,
                    CanReorder = o.Status == "Delivered" || o.Status == "Completed",
                    CanTrack = !string.IsNullOrEmpty(o.Shipment?.TrackingNumber) && (o.Status == "Shipped" || o.Status == "Processing")
                })
                .ToList();

            // Get addresses count
            var addressesCount = await _context.Addresses
                .Where(a => a.UserId == userId)
                .CountAsync();

            // Get personalized recommendations
            var recommendedProducts = await _recommendationService.GetPersonalizedRecommendationsAsync(userId, 6);

            // Create sample notifications (in a real app, these would come from a notifications table)
            var notifications = new List<DashboardNotification>();
            
            // Add order-related notifications
            var recentPendingOrder = orders.Where(o => o.Status == "Pending").OrderByDescending(o => o.CreatedAt).FirstOrDefault();
            if (recentPendingOrder != null)
            {
                notifications.Add(new DashboardNotification
                {
                    Id = 1,
                    Title = "Order Confirmation",
                    Message = $"Your order #{recentPendingOrder.OrderNumber} is being processed.",
                    Type = "info",
                    CreatedAt = recentPendingOrder.CreatedAt,
                    IsRead = false,
                    ActionUrl = Url.Action("OrderDetails", "Account", new { id = recentPendingOrder.Id }),
                    ActionText = "View Order"
                });
            }

            var recentShippedOrder = orders.Where(o => o.Status == "Shipped").OrderByDescending(o => o.UpdatedAt).FirstOrDefault();
            if (recentShippedOrder != null)
            {
                notifications.Add(new DashboardNotification
                {
                    Id = 2,
                    Title = "Order Shipped",
                    Message = $"Your order #{recentShippedOrder.OrderNumber} has been shipped!",
                    Type = "success",
                    CreatedAt = recentShippedOrder.UpdatedAt,
                    IsRead = false,
                    ActionUrl = Url.Action("OrderDetails", "Account", new { id = recentShippedOrder.Id }),
                    ActionText = "Track Package"
                });
            }

            // Add welcome notification for new users
            if ((DateTime.Now - user.CreatedAt).TotalDays <= 7)
            {
                notifications.Add(new DashboardNotification
                {
                    Id = 3,
                    Title = "Welcome to AccessoryWorld!",
                    Message = "Complete your profile to get personalized recommendations.",
                    Type = "info",
                    CreatedAt = user.CreatedAt,
                    IsRead = false,
                    ActionUrl = Url.Action("Profile", "Account"),
                    ActionText = "Complete Profile"
                });
            }

            var dashboardViewModel = new CustomerDashboardViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                CompletedOrders = completedOrders,
                SavedAddresses = addressesCount,
                WishlistItems = await _wishlistService.GetWishlistCountAsync(userId),
                TotalSpent = totalSpent,
                MemberSince = user.CreatedAt,
                LastOrderDate = lastOrderDate,
                LastLoginDate = user.LastLoginAt,
                RecentOrders = recentOrders,
                Notifications = notifications.OrderByDescending(n => n.CreatedAt).Take(5).ToList(),
                RecommendedProducts = recommendedProducts
            };

            return View("~/Views/Customer/Account/Dashboard.cshtml", dashboardViewModel);
        }

    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var orders = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.SKU)
                    .ThenInclude(s => s.Product)
                        .ThenInclude(p => p.ProductImages)
            .Include(o => o.ShippingAddress)
            .Include(o => o.Payments)
            .Include(o => o.Shipment)
            .Where(o => o.UserId == user.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        var orderViewModels = orders.Select(o => new OrderHistoryViewModel
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Status = o.Status,
            Total = o.Total,
            Currency = o.Currency,
            CreatedAt = o.CreatedAt,
            PaidAt = o.PaidAt,
            ShippedAt = o.ShippedAt,
            DeliveredAt = o.DeliveredAt,
            FulfilmentMethod = o.FulfilmentMethod,
            TrackingNumber = o.Shipment?.TrackingNumber,
            ShippingAddress = $"{o.ShippingAddress.AddressLine1}, {o.ShippingAddress.City}, {o.ShippingAddress.Province} {o.ShippingAddress.PostalCode}",
            ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
            OrderItems = o.OrderItems.Select(oi => new OrderItemViewModel
            {
                ProductName = oi.SKU.Product.Name,
                SKUCode = oi.SKU.SKUCode,
                Variant = oi.SKU.Variant,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                LineTotal = oi.LineTotal,
                Status = oi.Status,
                ProductImage = oi.SKU.Product.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageUrl
            }).ToList()
        }).ToList();

        return View("~/Views/Customer/Account/Orders.cshtml", orderViewModels);
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var order = await _context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.SKU)
                    .ThenInclude(s => s.Product)
                        .ThenInclude(p => p.ProductImages)
            .Include(o => o.ShippingAddress)
            .Include(o => o.Payments)
            .Include(o => o.Shipment)
            .Include(o => o.RMAs)
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == user.Id);

        if (order == null)
        {
            return NotFound();
        }

        var orderViewModel = new OrderDetailsViewModel
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status,
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            ShippingFee = order.ShippingFee,
            DiscountAmount = order.DiscountAmount,
            CreditNoteAmount = order.CreditNoteAmount,
            Total = order.Total,
            Currency = order.Currency,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            ShippedAt = order.ShippedAt,
            DeliveredAt = order.DeliveredAt,
            FulfilmentMethod = order.FulfilmentMethod,
            ShippingAddress = new AddressViewModel
            {
                AddressLine1 = order.ShippingAddress.AddressLine1,
                AddressLine2 = order.ShippingAddress.AddressLine2,
                City = order.ShippingAddress.City,
                Province = order.ShippingAddress.Province,
                PostalCode = order.ShippingAddress.PostalCode,
                Country = order.ShippingAddress.Country
            },
            Shipment = order.Shipment != null ? new ShipmentViewModel
            {
                CourierCode = order.Shipment.CourierCode,
                TrackingNumber = order.Shipment.TrackingNumber,
                Status = order.Shipment.Status,
                EstimatedDeliveryDate = order.Shipment.EstimatedDeliveryDate,
                ActualDeliveryDate = order.Shipment.ActualDeliveryDate
            } : null,
            OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
            {
                ProductName = oi.SKU.Product.Name,
                SKUCode = oi.SKU.SKUCode,
                Variant = oi.SKU.Variant,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                LineTotal = oi.LineTotal,
                Status = oi.Status,
                ProductImage = oi.SKU.Product.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageUrl
            }).ToList(),
            Payments = order.Payments.Select(p => new PaymentHistoryViewModel
            {
                Method = p.Method,
                Status = p.Status,
                Amount = p.Amount,
                RefundedAmount = p.RefundedAmount,
                Currency = p.Currency,
                CreatedAt = p.CreatedAt,
                ProcessedAt = p.ProcessedAt,
                FailureReason = p.FailureReason
            }).ToList(),
            HasActiveRMA = order.RMAs.Any(r => r.Status != "PROCESSED" && r.Status != "REFUNDED")
        };

        return View("~/Views/Customer/Account/OrderDetails.cshtml", orderViewModel);
    }
    }
}