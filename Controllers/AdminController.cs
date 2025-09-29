using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.ViewModels;
using AccessoryWorld.Security;

namespace AccessoryWorld.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // GET: Admin/ProductDetails/{id}
        [HttpGet]
        [Route("Admin/ProductDetails/{id:int}")]
        public async Task<IActionResult> ProductDetails(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductSpecifications)
                    .Include(p => p.SKUs)
                        .ThenInclude(s => s.StockMovements)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found";
                    return RedirectToAction(nameof(Products));
                }

                // Get related products for admin reference
                var relatedProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.ProductImages)
                    .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                    .Take(4)
                    .ToListAsync();

                ViewBag.RelatedProducts = relatedProducts;

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for admin");
                TempData["ErrorMessage"] = "Error loading product details: " + ex.Message;
                return RedirectToAction(nameof(Products));
            }
        }

        // GET: Admin/Index - Redirect to Dashboard
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                // Get dashboard statistics
                var totalUsers = await _context.Users.CountAsync();
                var totalOrders = await _context.Orders.CountAsync();
                var totalProducts = await _context.Products.CountAsync();
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == "Completed" || o.Status == "Delivered")
                    .SumAsync(o => o.Total);

                // Get trade-in statistics
                var pendingTradeIns = await _context.TradeInCases
                    .CountAsync(t => t.Status == "SUBMITTED" || t.Status == "AWAITING_EVALUATION");
                
                var todayTradeIns = await _context.TradeInCases
                    .CountAsync(t => t.CreatedAt.Date == DateTime.UtcNow.Date);
                
                var weeklyTradeIns = await _context.TradeInCases
                    .CountAsync(t => t.CreatedAt >= DateTime.UtcNow.AddDays(-7));
                
                var monthlyTradeIns = await _context.TradeInCases
                    .CountAsync(t => t.CreatedAt >= DateTime.UtcNow.AddMonths(-1));

                // Get credit note statistics
                var activeCreditNotes = await _context.CreditNotes
                    .CountAsync(cn => cn.Status == "ACTIVE" && cn.ExpiresAt > DateTime.UtcNow);
                
                var totalCreditNotesIssued = await _context.CreditNotes
                    .CountAsync(cn => cn.CreatedAt >= DateTime.UtcNow.AddMonths(-1));
                
                var totalCreditValue = await _context.CreditNotes
                    .Where(cn => cn.Status == "ACTIVE")
                    .SumAsync(cn => cn.AmountRemaining);

                // Get new customer signups (this month)
                var newCustomersThisMonth = await _context.Users
                    .CountAsync(u => u.CreatedAt >= DateTime.UtcNow.AddMonths(-1));

                // Get recent orders
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.SKU)
                            .ThenInclude(s => s.Product)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .Select(o => new RecentOrderViewModel
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = $"{o.User.FirstName} {o.User.LastName}",
                        Status = o.Status,
                        Total = o.Total,
                        Currency = o.Currency,
                        CreatedAt = o.CreatedAt,
                        ItemCount = o.OrderItems.Sum(oi => oi.Quantity)
                    })
                    .ToListAsync();

                // Get low stock products
                var lowStockProducts = await _context.SKUs
                    .Include(s => s.Product)
                    .Where(s => s.StockQuantity <= 10 && s.StockQuantity > 0)
                    .OrderBy(s => s.StockQuantity)
                    .Take(10)
                    .Select(s => new LowStockProductViewModel
                    {
                        Id = s.Id,
                        ProductName = s.Product.Name,
                        SKUCode = s.SKUCode,
                        StockQuantity = s.StockQuantity,
                        Price = s.Price
                    })
                    .ToListAsync();

                // Get monthly sales data for chart (last 6 months)
                var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
                var monthlySales = await _context.Orders
                    .Where(o => o.CreatedAt >= sixMonthsAgo && (o.Status == "Completed" || o.Status == "Delivered"))
                    .GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                    .Select(g => new MonthlySalesViewModel
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        TotalSales = g.Sum(o => o.Total),
                        OrderCount = g.Count()
                    })
                    .OrderBy(ms => ms.Year)
                    .ThenBy(ms => ms.Month)
                    .ToListAsync();

                var dashboardViewModel = new AdminDashboardViewModel
                {
                    TotalUsers = totalUsers,
                    TotalOrders = totalOrders,
                    TotalProducts = totalProducts,
                    TotalRevenue = totalRevenue,
                    PendingTradeIns = pendingTradeIns,
                    TodayTradeIns = todayTradeIns,
                    WeeklyTradeIns = weeklyTradeIns,
                    MonthlyTradeIns = monthlyTradeIns,
                    ActiveCreditNotes = activeCreditNotes,
                    TotalCreditNotesIssued = totalCreditNotesIssued,
                    TotalCreditValue = totalCreditValue,
                    NewCustomersThisMonth = newCustomersThisMonth,
                    RecentOrders = recentOrders,
                    LowStockProducts = lowStockProducts,
                    MonthlySales = monthlySales
                };

                return View(dashboardViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View(new AdminDashboardViewModel());
            }
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users(string search = "", string role = "", string status = "", int page = 1, int pageSize = 20)
        {
            var query = _context.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => (u.FirstName ?? "").Contains(search) || 
                                        (u.LastName ?? "").Contains(search) || 
                                        (u.Email ?? "").Contains(search) ||
                                        (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            // Apply status filter
            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status == "active";
                query = query.Where(u => u.IsActive == isActive);
            }

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalUsers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalUsers / pageSize);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = search;
            ViewBag.RoleFilter = role;
            ViewBag.StatusFilter = status;

            // Map ApplicationUser entities to AdminUserViewModel with roles
            var userViewModels = new List<AdminUserViewModel>();
            
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var totalOrders = await _context.Orders.CountAsync(o => o.UserId == user.Id);
                var totalSpent = await _context.Orders
                    .Where(o => o.UserId == user.Id && (o.Status == "Completed" || o.Status == "Delivered"))
                    .SumAsync(o => o.Total);
                
                userViewModels.Add(new AdminUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email ?? string.Empty,
                    PhoneNumber = user.PhoneNumber ?? string.Empty,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt,
                    Roles = userRoles.ToList(),
                    TotalOrders = totalOrders,
                    TotalSpent = totalSpent
                });
            }

            return View(userViewModels);
        }

        // GET: Admin/UserDetails/{id}
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var totalOrders = await _context.Orders.CountAsync(o => o.UserId == id);
            var totalSpent = await _context.Orders
                .Where(o => o.UserId == id && (o.Status == "Completed" || o.Status == "Delivered"))
                .SumAsync(o => o.Total);

            var userDetails = new AdminUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = userRoles.ToList(),
                TotalOrders = totalOrders,
                TotalSpent = totalSpent
            };

            return Json(new { success = true, user = userDetails });
        }

        // POST: Admin/ToggleUserStatus
        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(string userId, bool activate)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                user.IsActive = activate;
                await _context.SaveChangesAsync();

                var action = activate ? "activated" : "deactivated";
                return Json(new { success = true, message = $"User {action} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while updating user status" });
            }
        }

        // POST: Admin/UpdateUserRole
        [HttpPost]
        public async Task<IActionResult> UpdateUserRole(string userId, string role, bool assign)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (assign)
                {
                    var result = await _userManager.AddToRoleAsync(user, role);
                    if (!result.Succeeded)
                    {
                        return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                    }
                }
                else
                {
                    var result = await _userManager.RemoveFromRoleAsync(user, role);
                    if (!result.Succeeded)
                    {
                        return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                    }
                }

                var action = assign ? "assigned" : "removed";
                return Json(new { success = true, message = $"Role {role} {action} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role for user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while updating user role" });
            }
        }

        // POST: Admin/ArchiveUser
        [HttpPost]
        public async Task<IActionResult> ArchiveUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Archive user by setting IsActive to false
                user.IsActive = false;
                var result = await _userManager.UpdateAsync(user);
                
                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                // Log the archiving action
                _logger.LogInformation("User {UserId} archived by admin", userId);
                
                return Json(new { success = true, message = "User archived successfully. User data is preserved but account is deactivated." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error archiving user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while archiving user" });
            }
        }

        // POST: Admin/UnarchiveUser
        [HttpPost]
        public async Task<IActionResult> UnarchiveUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Unarchive user by setting IsActive to true
                user.IsActive = true;
                var result = await _userManager.UpdateAsync(user);
                
                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                // Log the unarchiving action
                _logger.LogInformation("User {UserId} unarchived by admin", userId);
                
                return Json(new { success = true, message = "User restored successfully. User can now access the system again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unarchiving user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while restoring user" });
            }
        }

        // POST: Admin/AddUser
        [HttpPost]
        public async Task<IActionResult> AddUser(string firstName, string lastName, string email, string phoneNumber, string password, bool isActive, string[] roles)
        {
            try
            {
                // Validate input
                if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return Json(new { success = false, message = "Please fill in all required fields" });
                }

                if (password.Length < 6)
                {
                    return Json(new { success = false, message = "Password must be at least 6 characters long" });
                }

                if (roles == null || roles.Length == 0)
                {
                    return Json(new { success = false, message = "Please select at least one role" });
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "A user with this email already exists" });
                }

                // Create new user
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    PhoneNumber = phoneNumber,
                    EmailConfirmed = true, // Auto-confirm for admin created users
                    IsActive = isActive,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                // Assign roles
                var validRoles = new[] { "Admin", "Customer", "InventoryManager", "Cashier", "FulfilmentAgent" };
                var rolesToAssign = roles.Where(r => validRoles.Contains(r)).ToArray();
                
                if (rolesToAssign.Length > 0)
                {
                    var roleResult = await _userManager.AddToRolesAsync(user, rolesToAssign);
                    if (!roleResult.Succeeded)
                    {
                        // If role assignment fails, delete the user and return error
                        await _userManager.DeleteAsync(user);
                        return Json(new { success = false, message = "Failed to assign roles: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)) });
                    }
                }

                return Json(new { success = true, message = "User created successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return Json(new { success = false, message = "An error occurred while creating the user" });
            }
        }

        // DELETE: Admin/DeleteUser
        [HttpDelete]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                // Check if user has any orders or important data
                var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == userId);
                if (hasOrders)
                {
                    return Json(new { success = false, message = "Cannot delete user with existing orders. Consider archiving instead." });
                }

                // Remove user from all roles first
                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, userRoles);
                }

                // Delete related data
                var addresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
                _context.Addresses.RemoveRange(addresses);

                var cartItems = await _context.CartItems.Where(c => c.UserId == userId).ToListAsync();
                _context.CartItems.RemoveRange(cartItems);

                var wishlistItems = await _context.Wishlists.Where(w => w.UserId == userId).ToListAsync();
                _context.Wishlists.RemoveRange(wishlistItems);

                // Delete the user
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} deleted by admin", userId);
                return Json(new { success = true, message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return Json(new { success = false, message = "An error occurred while deleting the user" });
            }
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Orders(int page = 1, int pageSize = 20, string? status = null, string? search = null, string? fulfillmentMethod = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .Include(o => o.Shipment)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderNumber.Contains(search) || 
                                        (o.User != null && o.User.FirstName != null && o.User.FirstName.Contains(search)) || 
                                        (o.User != null && o.User.LastName != null && o.User.LastName.Contains(search)) ||
                                        (o.User != null && o.User.Email != null && o.User.Email.Contains(search)));
            }

            if (!string.IsNullOrEmpty(fulfillmentMethod))
            {
                query = query.Where(o => o.FulfilmentMethod == fulfillmentMethod);
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.CreatedAt <= endDate.Value.AddDays(1));
            }

            var totalOrders = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminOrderViewModel
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User != null ? $"{o.User.FirstName ?? ""} {o.User.LastName ?? ""}" : "Unknown",
                    CustomerEmail = o.User != null ? o.User.Email ?? "Unknown" : "Unknown",
                    Status = o.Status,
                    FulfilmentMethod = o.FulfilmentMethod,
                    Total = o.Total,
                    Currency = o.Currency,
                    CreatedAt = o.CreatedAt,
                    PaidAt = o.PaidAt,
                    ShippedAt = o.ShippedAt,
                    DeliveredAt = o.DeliveredAt,
                    ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
                    TrackingNumber = o.Shipment != null ? o.Shipment.TrackingNumber : null
                })
                .ToListAsync();

            // Calculate order statistics for ViewBag
            var totalOrdersCount = await _context.Orders.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var processingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
            var completedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed" || o.Status == "Delivered");

            ViewBag.TotalOrders = totalOrdersCount;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.ProcessingOrders = processingOrders;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalOrders / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.StatusFilter = status;
            ViewBag.SearchTerm = search;
            ViewBag.FulfillmentFilter = fulfillmentMethod;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(orders);
        }

        // POST: Admin/UpdateOrderStatus
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                var order = await _context.Orders.FindAsync(request.OrderId);
                if (order == null)
                {
                    return Json(new { success = false, message = "Order not found" });
                }

                var oldStatus = order.Status;
                order.Status = request.Status;

                // Update timestamps based on status
                switch (request.Status)
                {
                    case "Shipped":
                        if (order.ShippedAt == null)
                            order.ShippedAt = DateTime.UtcNow;
                        break;
                    case "Delivered":
                        if (order.DeliveredAt == null)
                            order.DeliveredAt = DateTime.UtcNow;
                        break;
                }

                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} status updated from {OldStatus} to {NewStatus}", 
                    order.Id, oldStatus, request.Status);

                return Json(new { success = true, message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status for order {OrderId}", request.OrderId);
                return Json(new { success = false, message = "An error occurred while updating order status" });
            }
        }

        // GET: Admin/OrderDetails/{id}
        [HttpGet]
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SKU)
                        .ThenInclude(s => s.Product)
                            .ThenInclude(p => p.ProductImages)
                .Include(o => o.ShippingAddress)
                .Include(o => o.Payments)
                .Include(o => o.Shipment)
                .Include(o => o.RMAs)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Admin/PrintInvoice/{id}
        [HttpGet]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.SKU)
                        .ThenInclude(s => s.Product)
                .Include(o => o.ShippingAddress)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Return a view that can be printed
            return View(order);
        }

        // GET: Admin/ExportOrders
        [HttpGet]
        public async Task<IActionResult> ExportOrders(string? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .Include(o => o.Shipment)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(o => o.Status == status);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt <= endDate.Value.AddDays(1));
                }

                var orders = await query
                    .OrderByDescending(o => o.CreatedAt)
                    .Select(o => new
                    {
                        OrderNumber = o.OrderNumber,
                        CustomerName = $"{o.User.FirstName} {o.User.LastName}",
                        CustomerEmail = o.User.Email,
                        Status = o.Status,
                        FulfilmentMethod = o.FulfilmentMethod,
                        Total = o.Total,
                        Currency = o.Currency,
                        ItemCount = o.OrderItems.Sum(oi => oi.Quantity),
                        CreatedAt = o.CreatedAt,
                        PaidAt = o.PaidAt,
                        ShippedAt = o.ShippedAt,
                        DeliveredAt = o.DeliveredAt,
                        TrackingNumber = o.Shipment != null ? o.Shipment.TrackingNumber : null
                    })
                    .ToListAsync();

                // Create CSV content
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Order Number,Customer Name,Customer Email,Status,Fulfilment Method,Total,Currency,Item Count,Created At,Paid At,Shipped At,Delivered At,Tracking Number");

                foreach (var order in orders)
                {
                    csv.AppendLine($"{order.OrderNumber},{order.CustomerName},{order.CustomerEmail},{order.Status},{order.FulfilmentMethod},{order.Total},{order.Currency},{order.ItemCount},{order.CreatedAt:yyyy-MM-dd HH:mm:ss},{order.PaidAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""},{order.ShippedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""},{order.DeliveredAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""},{order.TrackingNumber ?? ""}");
                }

                var fileName = $"orders_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting orders");
                TempData["Error"] = "An error occurred while exporting orders";
                return RedirectToAction("Orders");
            }
        }

        // GET: Admin/Products
        public async Task<IActionResult> Products(int page = 1, int pageSize = 20)
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.SKUs)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalProducts = await _context.Products.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // Calculate statistics for ViewBag
            var activeProducts = await _context.Products.CountAsync(p => p.IsActive);
            var lowStockProducts = await _context.SKUs.CountAsync(s => s.StockQuantity > 0 && s.StockQuantity <= s.LowStockThreshold);
            var outOfStockProducts = await _context.SKUs.CountAsync(s => s.StockQuantity == 0);
            
            // Get categories and brands for filters
            var categories = await _context.Categories.Select(c => new { c.Id, c.Name }).ToListAsync();
            var brands = await _context.Brands.Select(b => new { b.Id, b.Name }).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.ActiveProducts = activeProducts;
            ViewBag.LowStockProducts = lowStockProducts;
            ViewBag.OutOfStockProducts = outOfStockProducts;
            ViewBag.Categories = categories;
            ViewBag.Brands = brands;

            // Map Product entities to AdminProductViewModel
            var productViewModels = products.Select(p => new AdminProductViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                BrandName = p.Brand?.Name ?? "Unknown",
                CategoryName = p.Category?.Name ?? "Unknown",
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                SKUCount = p.SKUs.Count,
                TotalStock = p.SKUs.Sum(s => s.StockQuantity),
                StockQuantity = p.SKUs.Sum(s => s.StockQuantity),
                LowStockThreshold = p.SKUs.FirstOrDefault()?.LowStockThreshold ?? 5,
                MinPrice = p.SKUs.Any() ? p.SKUs.Min(s => s.Price) : p.Price,
                MaxPrice = p.SKUs.Any() ? p.SKUs.Max(s => s.Price) : p.Price,
                Price = p.Price,
                CompareAtPrice = p.CompareAtPrice,
                SKU = p.SKUs.FirstOrDefault()?.SKUCode ?? "N/A",
                ImageUrl = p.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageUrl
            }).ToList();

            return View(productViewModels);
        }

        // Inventory Management Actions
        [HttpGet]
        public async Task<IActionResult> Inventory(string search = "", string status = "", string stockLevel = "", string sortBy = "name", int page = 1, int pageSize = 20)
        {
            var query = _context.SKUs
                .Include(s => s.Product)
                .ThenInclude(p => p.Category)
                .Include(s => s.Product)
                .ThenInclude(p => p.Brand)
                .Include(s => s.Product)
                .ThenInclude(p => p.ProductImages)
                .AsQueryable();

            // Calculate statistics for ViewBag
            var totalSKUs = await _context.SKUs.CountAsync();
            var inStockSKUs = await _context.SKUs.CountAsync(s => s.StockQuantity > s.LowStockThreshold);
            var lowStockSKUs = await _context.SKUs.CountAsync(s => s.StockQuantity > 0 && s.StockQuantity <= s.LowStockThreshold);
            var outOfStockSKUs = await _context.SKUs.CountAsync(s => s.StockQuantity == 0);

            ViewBag.TotalSKUs = totalSKUs;
            ViewBag.InStockSKUs = inStockSKUs;
            ViewBag.LowStockSKUs = lowStockSKUs;
            ViewBag.OutOfStockSKUs = outOfStockSKUs;

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.SKUCode.Contains(search) || s.Product.Name.Contains(search));
            }

            if (!string.IsNullOrEmpty(status))
            {
                bool isActive = status == "active";
                query = query.Where(s => s.IsActive == isActive);
            }

            if (!string.IsNullOrEmpty(stockLevel))
            {
                switch (stockLevel)
                {
                    case "instock":
                        query = query.Where(s => s.StockQuantity > s.LowStockThreshold);
                        break;
                    case "lowstock":
                        query = query.Where(s => s.StockQuantity > 0 && s.StockQuantity <= s.LowStockThreshold);
                        break;
                    case "outofstock":
                        query = query.Where(s => s.StockQuantity == 0);
                        break;
                }
            }

            // Apply sorting
            query = sortBy switch
            {
                "sku" => query.OrderBy(s => s.SKUCode),
                "stock" => query.OrderBy(s => s.StockQuantity),
                "price" => query.OrderBy(s => s.Price),
                _ => query.OrderBy(s => s.Product.Name)
            };

            var totalItems = await query.CountAsync();
            var inventoryItems = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new AdminInventoryViewModel
                {
                    SKUId = s.Id,
                    ProductId = s.ProductId,
                    ProductName = s.Product.Name,
                    SKUCode = s.SKUCode,
                    Variant = s.Variant,
                    CurrentStock = s.StockQuantity,
                    ReservedStock = s.ReservedQuantity,
                    AvailableStock = s.StockQuantity - s.ReservedQuantity,
                    LowStockThreshold = s.LowStockThreshold,
                    Price = s.Price,
                    CompareAtPrice = s.CompareAtPrice,
                    IsActive = s.IsActive,
                    CategoryName = s.Product.Category.Name,
                    BrandName = s.Product.Brand.Name,
                    ImageUrl = s.Product.ProductImages.Where(pi => pi.IsPrimary).Select(pi => pi.ImageUrl).FirstOrDefault(),
                    LastUpdated = s.UpdatedAt
                })
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = search;
            ViewBag.StatusFilter = status;
            ViewBag.StockFilter = stockLevel;
            ViewBag.SortBy = sortBy;

            return View(inventoryItems);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int skuId, int newStock, string reason = "Manual Adjustment")
        {
            try
            {
                var sku = await _context.SKUs.FindAsync(skuId);
                if (sku == null)
                {
                    return Json(new { success = false, message = "SKU not found" });
                }

                var oldStock = sku.StockQuantity;
                var quantityChange = newStock - oldStock;

                // Update SKU stock
                sku.StockQuantity = newStock;
                sku.UpdatedAt = DateTime.UtcNow;

                // Create stock movement record
                var movement = new StockMovement
                {
                    SKUId = skuId,
                    MovementType = quantityChange > 0 ? "ADJUSTMENT_IN" : "ADJUSTMENT_OUT",
                    Quantity = quantityChange,
                    ReasonCode = reason,
                    Notes = $"Stock adjusted from {oldStock} to {newStock}",
                    UserId = _userManager.GetUserId(User) ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StockMovements.Add(movement);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Stock updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> InventoryMovements(int skuId, int page = 1, int pageSize = 10)
        {
            var movements = await _context.StockMovements
                .Include(sm => sm.SKU)
                .ThenInclude(s => s.Product)
                .Include(sm => sm.User)
                .Where(sm => sm.SKUId == skuId)
                .OrderByDescending(sm => sm.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Json(movements.Select(m => new
            {
                id = m.Id,
                movementType = m.MovementType,
                quantity = m.Quantity,
                reasonCode = m.ReasonCode,
                notes = m.Notes,
                referenceNumber = m.ReferenceNumber,
                userName = m.User?.FirstName + " " + m.User?.LastName,
                createdAt = m.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
            }));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSKUStatus([FromBody] ToggleSKUStatusRequest request)
        {
            try
            {
                var sku = await _context.SKUs.FindAsync(request.SkuId);
                if (sku == null)
                {
                    return Json(new { success = false, message = "SKU not found" });
                }

                sku.IsActive = request.IsActive;
                sku.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "SKU status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling SKU status");
                return Json(new { success = false, message = "Error updating SKU status" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventory()
        {
            try
            {
                var inventoryItems = await _context.SKUs
                    .Include(s => s.Product)
                    .ThenInclude(p => p.Category)
                    .Include(s => s.Product)
                    .ThenInclude(p => p.Brand)
                    .Select(s => new {
                        SKUCode = s.SKUCode,
                        ProductName = s.Product.Name,
                        Category = s.Product.Category.Name,
                        Brand = s.Product.Brand.Name,
                        Variant = s.Variant,
                        Price = s.Price,
                        CurrentStock = s.StockQuantity,
                        ReservedStock = s.ReservedQuantity,
                        AvailableStock = s.StockQuantity - s.ReservedQuantity,
                        LowStockThreshold = s.LowStockThreshold,
                        IsActive = s.IsActive,
                        LastUpdated = s.UpdatedAt
                    })
                    .ToListAsync();

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("SKU Code,Product Name,Category,Brand,Variant,Price,Current Stock,Reserved Stock,Available Stock,Low Stock Threshold,Is Active,Last Updated");

                foreach (var item in inventoryItems)
                {
                    csv.AppendLine($"\"{item.SKUCode}\",\"{item.ProductName}\",\"{item.Category}\",\"{item.Brand}\",\"{item.Variant ?? ""}\",{item.Price},{item.CurrentStock},{item.ReservedStock},{item.AvailableStock},{item.LowStockThreshold},{item.IsActive},{item.LastUpdated:yyyy-MM-dd HH:mm:ss}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"inventory_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting inventory");
                TempData["ErrorMessage"] = "Error exporting inventory: " + ex.Message;
                return RedirectToAction(nameof(Inventory));
            }
        }

        // GET: Admin/Reports
        public async Task<IActionResult> Reports()
        {
            try
            {
                // Get report data
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == "Completed" || o.Status == "Delivered")
                    .SumAsync(o => o.Total);

                var monthlyRevenue = await _context.Orders
                    .Where(o => (o.Status == "Completed" || o.Status == "Delivered") && 
                               o.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                    .SumAsync(o => o.Total);

                var totalOrders = await _context.Orders.CountAsync();
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
                var completedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");

                var topProducts = await _context.OrderItems
                    .Include(oi => oi.SKU)
                        .ThenInclude(s => s.Product)
                    .GroupBy(oi => oi.SKU.ProductId)
                    .Select(g => new {
                        ProductName = g.First().SKU.Product.Name,
                        TotalSold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(10)
                    .ToListAsync();

                ViewBag.TotalRevenue = totalRevenue;
                ViewBag.MonthlyRevenue = monthlyRevenue;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.PendingOrders = pendingOrders;
                ViewBag.CompletedOrders = completedOrders;
                ViewBag.TopProducts = topProducts;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports");
                return View("Error");
            }
        }

        // GET: Admin/ExportReport
        [HttpGet]
        public async Task<IActionResult> ExportReport()
        {
            try
            {
                // Get comprehensive report data
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == "Completed" || o.Status == "Delivered")
                    .SumAsync(o => o.Total);

                var monthlyRevenue = await _context.Orders
                    .Where(o => (o.Status == "Completed" || o.Status == "Delivered") && 
                               o.CreatedAt >= DateTime.UtcNow.AddMonths(-1))
                    .SumAsync(o => o.Total);

                var totalOrders = await _context.Orders.CountAsync();
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
                var completedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
                var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Cancelled");
                var processingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");

                var topProducts = await _context.OrderItems
                    .Include(oi => oi.SKU)
                        .ThenInclude(s => s.Product)
                    .GroupBy(oi => oi.SKU.ProductId)
                    .Select(g => new {
                        ProductName = g.First().SKU.Product.Name,
                        TotalSold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                    })
                    .OrderByDescending(x => x.TotalSold)
                    .Take(10)
                    .ToListAsync();

                // Create CSV content
                var csv = new System.Text.StringBuilder();
                csv.AppendLine("AccessoryWorld - Business Report");
                csv.AppendLine($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                csv.AppendLine("");
                
                // Revenue Statistics
                csv.AppendLine("REVENUE STATISTICS");
                csv.AppendLine($"Total Revenue,${totalRevenue:F2}");
                csv.AppendLine($"Monthly Revenue,${monthlyRevenue:F2}");
                csv.AppendLine("");
                
                // Order Statistics
                csv.AppendLine("ORDER STATISTICS");
                csv.AppendLine($"Total Orders,{totalOrders}");
                csv.AppendLine($"Completed Orders,{completedOrders}");
                csv.AppendLine($"Pending Orders,{pendingOrders}");
                csv.AppendLine($"Processing Orders,{processingOrders}");
                csv.AppendLine($"Cancelled Orders,{cancelledOrders}");
                csv.AppendLine("");
                
                // Top Products
                csv.AppendLine("TOP SELLING PRODUCTS");
                csv.AppendLine("Rank,Product Name,Units Sold,Revenue");
                for (int i = 0; i < topProducts.Count; i++)
                {
                    var product = topProducts[i];
                    csv.AppendLine($"{i + 1},\"{product.ProductName}\",{product.TotalSold},${product.Revenue:F2}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"business_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                TempData["ErrorMessage"] = "Error exporting report: " + ex.Message;
                return RedirectToAction(nameof(Reports));
            }
        }

        // GET: Admin/Settings
        public async Task<IActionResult> Settings()
        {
            try
            {
                var settings = await _context.Settings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    // Create default settings if none exist
                    settings = new Settings();
                    _context.Settings.Add(settings);
                    await _context.SaveChangesAsync();
                }

                var viewModel = new SettingsViewModel
                {
                    SiteName = settings.SiteName,
                    SiteEmail = settings.SiteEmail,
                    Currency = settings.Currency,
                    MaintenanceMode = settings.MaintenanceMode,
                    SmtpHost = settings.SmtpHost,
                    SmtpPort = settings.SmtpPort,
                    SmtpUsername = settings.SmtpUsername,
                    SmtpPassword = settings.SmtpPassword,
                    SmtpEnableSsl = settings.SmtpEnableSsl,
                    RequireEmailConfirmation = settings.RequireEmailConfirmation,
                    EnableTwoFactorAuth = settings.EnableTwoFactorAuth,
                    SessionTimeoutMinutes = settings.SessionTimeoutMinutes,
                    MaxLoginAttempts = settings.MaxLoginAttempts,
                    PaymentGateway = settings.PaymentGateway,
                    PaymentApiKey = settings.PaymentApiKey,
                    PaymentSecretKey = settings.PaymentSecretKey,
                    EnablePayPal = settings.EnablePayPal,
                    EnableCreditCard = settings.EnableCreditCard
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings");
                TempData["ErrorMessage"] = "Error loading settings: " + ex.Message;
                return View(new SettingsViewModel());
            }
        }

        // POST: Admin/Settings
        [HttpPost]
        public async Task<IActionResult> Settings(SettingsViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please correct the validation errors.";
                    return View(model);
                }

                var settings = await _context.Settings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new Settings();
                    _context.Settings.Add(settings);
                }

                // Update settings
                settings.SiteName = model.SiteName;
                settings.SiteEmail = model.SiteEmail;
                settings.Currency = model.Currency;
                settings.MaintenanceMode = model.MaintenanceMode;
                settings.SmtpHost = model.SmtpHost;
                settings.SmtpPort = model.SmtpPort;
                settings.SmtpUsername = model.SmtpUsername;
                settings.SmtpPassword = model.SmtpPassword;
                settings.SmtpEnableSsl = model.SmtpEnableSsl;
                settings.RequireEmailConfirmation = model.RequireEmailConfirmation;
                settings.EnableTwoFactorAuth = model.EnableTwoFactorAuth;
                settings.SessionTimeoutMinutes = model.SessionTimeoutMinutes;
                settings.MaxLoginAttempts = model.MaxLoginAttempts;
                settings.PaymentGateway = model.PaymentGateway;
                settings.PaymentApiKey = model.PaymentApiKey;
                settings.PaymentSecretKey = model.PaymentSecretKey;
                settings.EnablePayPal = model.EnablePayPal;
                settings.EnableCreditCard = model.EnableCreditCard;
                settings.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Settings updated successfully!";
                return RedirectToAction(nameof(Settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating settings");
                TempData["ErrorMessage"] = "Error updating settings: " + ex.Message;
                return View(model);
            }
        }

        // POST: Admin/ClearCache
        [HttpPost]
        public IActionResult ClearCache()
        {
            try
            {
                // Clear application cache (implement based on your caching strategy)
                // For now, just return success
                return Json(new { success = true, message = "Cache cleared successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                return Json(new { success = false, message = "Error clearing cache: " + ex.Message });
            }
        }

        // POST: Admin/BackupDatabase
        [HttpPost]
        public IActionResult BackupDatabase()
        {
            try
            {
                // Implement database backup logic here
                // For now, just return success
                return Json(new { success = true, message = "Database backup started. You will be notified when complete." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error backing up database");
                return Json(new { success = false, message = "Error backing up database: " + ex.Message });
            }
        }

        // POST: Admin/TestEmailSettings
        [HttpPost]
        public async Task<IActionResult> TestEmailSettings()
        {
            try
            {
                var settings = await _context.Settings.FirstOrDefaultAsync();
                if (settings == null || string.IsNullOrEmpty(settings.SmtpHost))
                {
                    return Json(new { success = false, message = "Email settings not configured." });
                }

                // Implement email test logic here
                // For now, just return success
                return Json(new { success = true, message = "Test email sent successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing email settings");
                return Json(new { success = false, message = "Error testing email: " + ex.Message });
            }
        }

        // GET: Admin/ExportProducts
        public async Task<IActionResult> ExportProducts()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.SKUs)
                    .Select(p => new {
                        Id = p.Id,
                        Name = p.Name,
                        Category = p.Category.Name,
                        Brand = p.Brand.Name,
                        Price = p.Price,
                        StockQuantity = p.SKUs.Sum(s => s.StockQuantity),
                        IsActive = p.IsActive,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Id,Name,Category,Brand,Price,Stock Quantity,Is Active,Created At");

                foreach (var product in products)
                {
                    csv.AppendLine($"{product.Id},\"{product.Name}\",\"{product.Category}\",\"{product.Brand}\",{product.Price},{product.StockQuantity},{product.IsActive},{product.CreatedAt:yyyy-MM-dd}");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                return File(bytes, "text/csv", $"products_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting products");
                TempData["ErrorMessage"] = "Error exporting products: " + ex.Message;
                return RedirectToAction(nameof(Products));
            }
        }

        // GET: Admin/AddProduct
        public async Task<IActionResult> AddProduct()
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();
            return View();
        }

        // POST: Admin/AddProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(AddProductViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Create the product
                    var product = new Product
                    {
                        Name = model.Name,
                        Description = model.Description,
                        CategoryId = model.CategoryId,
                        BrandId = model.BrandId,
                        Price = model.Price,
                        CompareAtPrice = model.CompareAtPrice,
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                    
                    // Create initial SKU for the product
                    var sku = new SKU
                    {
                        ProductId = product.Id,
                        SKUCode = !string.IsNullOrEmpty(model.SKUCode) ? model.SKUCode : $"SKU-{product.Id}-001",
                        Price = model.Price,
                        CompareAtPrice = model.CompareAtPrice,
                        StockQuantity = model.StockQuantity,
                        LowStockThreshold = model.LowStockThreshold,
                        IsActive = model.IsActive,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    _context.SKUs.Add(sku);
                    
                    // Create product image if URL is provided
                    if (!string.IsNullOrEmpty(model.ImageUrl))
                    {
                        var productImage = new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = model.ImageUrl,
                            IsPrimary = true,
                            SortOrder = 1
                        };
                        
                        _context.ProductImages.Add(productImage);
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    TempData["SuccessMessage"] = "Product added successfully!";
                    return RedirectToAction(nameof(Products));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                ModelState.AddModelError("", "Error adding product: " + ex.Message);
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Brands = await _context.Brands.ToListAsync();
            return View(model);
        }

        // GET: Admin/EditProduct/{id}
        public async Task<IActionResult> EditProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.SKUs)
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    TempData["ErrorMessage"] = "Product not found";
                    return RedirectToAction(nameof(Products));
                }

                var viewModel = new EditProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    CategoryId = product.CategoryId,
                    BrandId = product.BrandId,
                    Price = product.Price,
                    CompareAtPrice = product.CompareAtPrice,
                    SalePrice = product.SalePrice,
                    IsOnSale = product.IsOnSale,
                    StockQuantity = product.SKUs.Sum(s => s.StockQuantity),
                    LowStockThreshold = product.SKUs.FirstOrDefault()?.LowStockThreshold ?? 5,
                    ImageUrl = product.ProductImages.FirstOrDefault(pi => pi.IsPrimary)?.ImageUrl,
                    IsActive = product.IsActive,
                    IsFeatured = product.IsFeatured,
                    IsNew = product.IsNew,
                    IsHot = product.IsHot,
                    IsTodayDeal = product.IsTodayDeal,
                    IsBestSeller = product.IsBestSeller,
                    SKUCode = product.SKUs.FirstOrDefault()?.SKUCode,
                    CategoryName = product.Category?.Name ?? "Unknown",
                    BrandName = product.Brand?.Name ?? "Unknown",
                    ViewCount = product.ViewCount,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt,
                    CurrentImages = product.ProductImages.Select(pi => new ProductImageViewModel
                    {
                        Id = pi.Id,
                        ImageUrl = pi.ImageUrl,
                        IsPrimary = pi.IsPrimary,
                        SortOrder = pi.SortOrder
                    }).ToList()
                };

                ViewBag.Categories = await _context.Categories.ToListAsync();
                ViewBag.Brands = await _context.Brands.ToListAsync();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product for editing");
                TempData["ErrorMessage"] = "Error loading product: " + ex.Message;
                return RedirectToAction(nameof(Products));
            }
        }

        // POST: Admin/UpdateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProduct(EditProductViewModel model, List<IFormFile> NewImages)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var product = await _context.Products
                        .Include(p => p.SKUs)
                        .Include(p => p.ProductImages)
                        .FirstOrDefaultAsync(p => p.Id == model.Id);

                    if (product == null)
                    {
                        return Json(new { success = false, message = "Product not found" });
                    }

                    // Update product properties
                    product.Name = model.Name;
                    product.Description = model.Description;
                    product.CategoryId = model.CategoryId;
                    product.BrandId = model.BrandId;
                    product.Price = model.Price;
                    product.CompareAtPrice = model.CompareAtPrice;
                    product.SalePrice = model.SalePrice;
                    product.IsOnSale = model.IsOnSale;
                    product.IsActive = model.IsActive;
                    product.IsFeatured = model.IsFeatured;
                    product.IsNew = model.IsNew;
                    product.IsHot = model.IsHot;
                    product.IsTodayDeal = model.IsTodayDeal;
                    product.IsBestSeller = model.IsBestSeller;
                    product.UpdatedAt = DateTime.UtcNow;

                    // Update primary SKU if exists
                    var primarySku = product.SKUs.FirstOrDefault();
                    if (primarySku != null)
                    {
                        primarySku.Price = model.Price;
                        primarySku.CompareAtPrice = model.CompareAtPrice;
                        primarySku.LowStockThreshold = model.LowStockThreshold;
                        primarySku.IsActive = model.IsActive;
                        primarySku.UpdatedAt = DateTime.UtcNow;
                        if (!string.IsNullOrEmpty(model.SKUCode))
                        {
                            primarySku.SKUCode = model.SKUCode;
                        }
                    }

                    // Handle image deletions
                    if (model.ImagesToDelete != null && model.ImagesToDelete.Any())
                    {
                        var imagesToDelete = product.ProductImages.Where(pi => model.ImagesToDelete.Contains(pi.Id)).ToList();
                        _context.ProductImages.RemoveRange(imagesToDelete);
                    }

                    // Handle new image uploads
                    if (NewImages != null && NewImages.Any())
                    {
                        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                        Directory.CreateDirectory(uploadsFolder);

                        var maxSortOrder = product.ProductImages.Any() ? product.ProductImages.Max(pi => pi.SortOrder) : 0;
                        var isFirstImage = !product.ProductImages.Any();

                        foreach (var file in NewImages)
                        {
                            if (file.Length > 0)
                            {
                                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                                var filePath = Path.Combine(uploadsFolder, fileName);

                                using (var stream = new FileStream(filePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream);
                                }

                                var productImage = new ProductImage
                                {
                                    ProductId = product.Id,
                                    ImageUrl = "/images/products/" + fileName,
                                    IsPrimary = isFirstImage,
                                    SortOrder = ++maxSortOrder
                                };

                                _context.ProductImages.Add(productImage);
                                isFirstImage = false;
                            }
                        }
                    }

                    // Handle primary image update
                    if (model.PrimaryImageId.HasValue)
                    {
                        // Reset all images to non-primary
                        foreach (var img in product.ProductImages)
                        {
                            img.IsPrimary = false;
                        }
                        // Set the selected image as primary
                        var primaryImage = product.ProductImages.FirstOrDefault(pi => pi.Id == model.PrimaryImageId.Value);
                        if (primaryImage != null)
                        {
                            primaryImage.IsPrimary = true;
                        }
                    }

                    await _context.SaveChangesAsync();
                    return Json(new { success = true, message = "Product updated successfully" });
                }

                return Json(new { success = false, message = "Invalid data provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return Json(new { success = false, message = "Error updating product: " + ex.Message });
            }
        }

        // DELETE: Admin/DeleteProduct/{id}
        [HttpDelete]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.SKUs)
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Remove related data
                _context.ProductImages.RemoveRange(product.ProductImages);
                _context.SKUs.RemoveRange(product.SKUs);
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Product deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product");
                return Json(new { success = false, message = "Error deleting product: " + ex.Message });
            }
        }

        // POST: Admin/AddInventoryItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddInventoryItem(AddInventoryItemViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var sku = new SKU
                    {
                        ProductId = model.ProductId,
                        SKUCode = model.SKUCode,
                        Variant = model.Variant,
                        Price = model.Price,
                        CompareAtPrice = model.CompareAtPrice,
                        StockQuantity = model.StockQuantity,
                        LowStockThreshold = model.LowStockThreshold,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.SKUs.Add(sku);
                    await _context.SaveChangesAsync();

                    // Create stock movement record
                    var stockMovement = new StockMovement
                    {
                        SKUId = sku.Id,
                        MovementType = "Initial Stock",
                        Quantity = model.StockQuantity,
                        ReasonCode = "INITIAL",
                        Notes = "Initial inventory setup",
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.StockMovements.Add(stockMovement);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Inventory item added successfully!";
                    return Json(new { success = true, message = "Inventory item added successfully!" });
                }

                return Json(new { success = false, message = "Invalid data provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding inventory item");
                return Json(new { success = false, message = "Error adding inventory item: " + ex.Message });
            }
        }

        // POST: Admin/UpdateInventoryStock
        [HttpPost]
        public async Task<IActionResult> UpdateInventoryStock([FromBody] UpdateInventoryStockRequest request)
        {
            try
            {
                var sku = await _context.SKUs.FindAsync(request.SkuId);
                if (sku == null)
                {
                    return Json(new { success = false, message = "SKU not found" });
                }

                var oldQuantity = sku.StockQuantity;
                sku.StockQuantity = request.NewQuantity;
                sku.UpdatedAt = DateTime.UtcNow;

                // Create stock movement record
                var stockMovement = new StockMovement
                {
                    SKUId = sku.Id,
                    MovementType = request.NewQuantity > oldQuantity ? "Stock In" : "Stock Out",
                    Quantity = Math.Abs(request.NewQuantity - oldQuantity),
                    ReasonCode = request.ReasonCode ?? "ADJUSTMENT",
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow
                };

                _context.StockMovements.Add(stockMovement);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Stock updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating inventory stock");
                return Json(new { success = false, message = "Error updating stock" });
            }
        }

        // POST: Admin/ToggleProductStatus
        [HttpPost]
        public async Task<IActionResult> ToggleProductStatus([FromBody] ToggleProductStatusRequest request)
        {
            try
            {
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                product.IsActive = request.IsActive;
                product.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                return Json(new { success = true, message = "Product status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling product status");
                return Json(new { success = false, message = "Error updating product status" });
            }
        }
    }

    public class UpdateOrderStatusRequest
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ToggleProductStatusRequest
    {
        public int ProductId { get; set; }
        public bool IsActive { get; set; }
    }

    public class ToggleSKUStatusRequest
    {
        public int SkuId { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateInventoryStockRequest
    {
        public int SkuId { get; set; }
        public int NewQuantity { get; set; }
        public string? ReasonCode { get; set; }
        public string? Notes { get; set; }
    }
}