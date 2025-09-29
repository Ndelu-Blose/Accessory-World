using Microsoft.AspNetCore.Mvc;
using AccessoryWorld.Data;
using AccessoryWorld.Services;
using AccessoryWorld.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AccessoryWorld.Controllers
{
    public class DebugController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AddressService _addressService;
        private readonly ILogger<DebugController> _logger;

        public DebugController(ApplicationDbContext context, AddressService addressService, ILogger<DebugController> logger)
        {
            _context = context;
            _addressService = addressService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult TestAddress()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TestAddressPost()
        {
            try
            {
                _logger.LogInformation("=== DEBUG: Testing Address Addition ===");
                
                // Get the first user from the database
                var firstUser = await _context.Users.FirstOrDefaultAsync();
                if (firstUser == null)
                {
                    return Json(new { success = false, message = "No users found in database" });
                }

                _logger.LogInformation("Debug: Using user {UserId} ({Email})", firstUser.Id, firstUser.Email);

                // Create test address
                var testAddress = new AddressViewModel
                {
                    FullName = "Debug Test User",
                    PhoneNumber = "0123456789",
                    AddressLine1 = "123 Debug Street",
                    AddressLine2 = "Apt 1",
                    City = "Debug City",
                    Province = "Debug Province",
                    PostalCode = "12345",
                    Country = "South Africa",
                    IsDefault = true
                };

                _logger.LogInformation("Debug: Created test address model");

                var publicId = await _addressService.AddAddressAsync(testAddress, firstUser.Id);

                _logger.LogInformation("Debug: AddAddressAsync returned: {PublicId}", publicId);

                if (publicId != Guid.Empty)
                {
                    return Json(new { 
                        success = true, 
                        message = "Address added successfully!",
                        publicId = publicId.ToString(),
                        userId = firstUser.Id
                    });
                }
                else
                {
                    return Json(new { 
                        success = false, 
                        message = "Failed to add address - check logs for details",
                        userId = firstUser.Id
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Debug: Exception in TestAddressPost");
                return Json(new { 
                    success = false, 
                    message = $"Exception: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                var userCount = await _context.Users.CountAsync();
                var addressCount = await _context.Addresses.CountAsync();
                
                var users = await _context.Users.Take(3).Select(u => new { u.Id, u.Email }).ToListAsync();
                var addresses = await _context.Addresses.Take(5).Select(a => new { 
                    a.Id, a.PublicId, a.UserId, a.FullName, a.City, a.IsDefault 
                }).ToListAsync();

                return Json(new {
                    userCount,
                    addressCount,
                    users,
                    addresses
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}