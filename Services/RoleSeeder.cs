using Microsoft.AspNetCore.Identity;
using AccessoryWorld.Models;

namespace AccessoryWorld.Services
{
    public class RoleSeeder
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RoleSeeder> _logger;

        public RoleSeeder(
            RoleManager<IdentityRole> roleManager,
            UserManager<ApplicationUser> userManager,
            ILogger<RoleSeeder> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task SeedRolesAsync()
        {
            var roles = new[] { "Admin", "Customer", "InventoryManager", "SalesAssociate", "Cashier", "FulfilmentAgent" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Role '{role}' created successfully.");
                    }
                    else
                    {
                        _logger.LogError($"Failed to create role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        public async Task SeedAdminUserAsync()
        {
            var adminEmail = "Admin@Accessory.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    _logger.LogInformation("Admin user created successfully.");
                }
                else
                {
                    _logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}