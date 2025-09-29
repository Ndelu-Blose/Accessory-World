using AccessoryWorld.Data;
using AccessoryWorld.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccessoryWorld.Services
{
    public class AddressSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AddressSeeder> _logger;

        public AddressSeeder(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<AddressSeeder> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Check if addresses already exist
                if (await _context.Addresses.AnyAsync())
                {
                    _logger.LogInformation("Address data already exists, skipping seeding.");
                    return;
                }

                // Find customer users to add addresses for
                var customerUsers = await _userManager.GetUsersInRoleAsync("Customer");
                
                if (!customerUsers.Any())
                {
                    _logger.LogWarning("No customer users found. Creating sample customer users first.");
                    await CreateSampleCustomerUsersAsync();
                    customerUsers = await _userManager.GetUsersInRoleAsync("Customer");
                }

                var addresses = new List<Address>();

                // Create sample addresses for each customer
                foreach (var user in customerUsers.Take(3)) // Limit to first 3 customers
                {
                    addresses.AddRange(CreateSampleAddressesForUser(user.Id));
                }

                await _context.Addresses.AddRangeAsync(addresses);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully seeded {addresses.Count} sample addresses for {customerUsers.Count()} customers.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding address data.");
                throw;
            }
        }

        private async Task CreateSampleCustomerUsersAsync()
        {
            var sampleCustomers = new[]
            {
                new { Email = "john.doe@example.com", FirstName = "John", LastName = "Doe" },
                new { Email = "jane.smith@example.com", FirstName = "Jane", LastName = "Smith" },
                new { Email = "mike.johnson@example.com", FirstName = "Mike", LastName = "Johnson" }
            };

            foreach (var customer in sampleCustomers)
            {
                var existingUser = await _userManager.FindByEmailAsync(customer.Email);
                if (existingUser == null)
                {
                    var user = new ApplicationUser
                    {
                        UserName = customer.Email,
                        Email = customer.Email,
                        FirstName = customer.FirstName,
                        LastName = customer.LastName,
                        EmailConfirmed = true
                    };

                    var result = await _userManager.CreateAsync(user, "Customer123!");
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "Customer");
                        _logger.LogInformation($"Created sample customer user: {customer.Email}");
                    }
                    else
                    {
                        _logger.LogError($"Failed to create customer user {customer.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private List<Address> CreateSampleAddressesForUser(string userId)
        {
            return new List<Address>
            {
                new Address
                {
                    UserId = userId,
                    FullName = "John Doe",
                    AddressLine1 = "123 Main Street",
                    AddressLine2 = "Apartment 4B",
                    City = "Cape Town",
                    Province = "Western Cape",
                    PostalCode = "8001",
                    Country = "South Africa",
                    PhoneNumber = "+27 21 123 4567",
                    IsDefault = true
                },
                new Address
                {
                    UserId = userId,
                    FullName = "John Doe",
                    AddressLine1 = "456 Business Park Drive",
                    City = "Johannesburg",
                    Province = "Gauteng",
                    PostalCode = "2001",
                    Country = "South Africa",
                    PhoneNumber = "+27 11 987 6543",
                    IsDefault = false
                },
                new Address
                {
                    UserId = userId,
                    FullName = "Jane Smith",
                    AddressLine1 = "789 Ocean View Road",
                    City = "Durban",
                    Province = "KwaZulu-Natal",
                    PostalCode = "4001",
                    Country = "South Africa",
                    PhoneNumber = "+27 31 555 0123",
                    IsDefault = false
                },
                new Address
                {
                    UserId = userId,
                    FullName = "Mike Johnson",
                    AddressLine1 = "321 Garden Route Avenue",
                    AddressLine2 = "Unit 12",
                    City = "Port Elizabeth",
                    Province = "Eastern Cape",
                    PostalCode = "6001",
                    Country = "South Africa",
                    PhoneNumber = "+27 41 444 5678",
                    IsDefault = false
                }
            };
        }
    }
}