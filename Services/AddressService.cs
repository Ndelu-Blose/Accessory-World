using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace AccessoryWorld.Services
{
    public interface IAddressService
    {
        Task<List<AddressViewModel>> GetUserAddressesAsync(string userId);
        Task<AddressViewModel?> GetAddressByPublicIdAsync(Guid publicId, string userId);
        Task<Guid> AddAddressAsync(AddressViewModel model, string userId);
        Task<bool> UpdateAddressAsync(AddressViewModel model, string userId);
        Task<bool> DeleteAddressByPublicIdAsync(Guid publicId, string userId);
        Task<bool> SetDefaultAddressByPublicIdAsync(Guid publicId, string userId);
        Task<AddressViewModel> CreateAddressViewModelForUserAsync(string userId);
    }

    public class AddressService : IAddressService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AddressService> _logger;

        public AddressService(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AddressService> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<List<AddressViewModel>> GetUserAddressesAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting addresses for user {UserId}", userId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetUserAddressesAsync called with null or empty userId");
                    return new List<AddressViewModel>();
                }

                var addresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.IsDefault)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} addresses for user {UserId}", addresses.Count, userId);

                return addresses.Select(a => new AddressViewModel
                {
                    Id = a.Id,
                    PublicId = a.PublicId,
                    FullName = a.FullName,
                    PhoneNumber = a.PhoneNumber,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    City = a.City,
                    Province = a.Province,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    IsDefault = a.IsDefault
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving addresses for user {UserId}", userId);
                return new List<AddressViewModel>();
            }
        }

        public async Task<AddressViewModel?> GetAddressByPublicIdAsync(Guid publicId, string userId)
        {
            try
            {
                _logger.LogInformation("Getting address {PublicId} for user {UserId}", publicId, userId);

                if (publicId == Guid.Empty)
                {
                    _logger.LogWarning("GetAddressByPublicIdAsync called with empty PublicId");
                    return null;
                }

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("GetAddressByPublicIdAsync called with null or empty userId");
                    return null;
                }

                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.PublicId == publicId && a.UserId == userId);

                if (address == null)
                {
                    _logger.LogWarning("Address {PublicId} not found for user {UserId}", publicId, userId);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved address {PublicId} for user {UserId}", publicId, userId);

                return new AddressViewModel
                {
                    Id = address.Id,
                    PublicId = address.PublicId,
                    FullName = address.FullName,
                    PhoneNumber = address.PhoneNumber,
                    AddressLine1 = address.AddressLine1,
                    AddressLine2 = address.AddressLine2,
                    City = address.City,
                    Province = address.Province,
                    PostalCode = address.PostalCode,
                    Country = address.Country,
                    IsDefault = address.IsDefault
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving address {PublicId} for user {UserId}", publicId, userId);
                return null;
            }
        }

        public async Task<Guid> AddAddressAsync(AddressViewModel model, string userId)
        {
            try
            {
                _logger.LogInformation("=== STARTING AddAddressAsync (Phase 3) ===");
                _logger.LogInformation("User ID: {UserId}", userId);
                _logger.LogInformation("Model data: {@Model}", new {
                    FullName = model.FullName,
                    PhoneNumber = model.PhoneNumber,
                    AddressLine1 = model.AddressLine1,
                    AddressLine2 = model.AddressLine2,
                    City = model.City,
                    Province = model.Province,
                    PostalCode = model.PostalCode,
                    Country = model.Country,
                    IsDefault = model.IsDefault
                });

                // Validate inputs
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("AddAddressAsync called with null or empty userId");
                    return Guid.Empty;
                }

                if (model == null)
                {
                    _logger.LogError("AddAddressAsync called with null model");
                    return Guid.Empty;
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(model.FullName) ||
                    string.IsNullOrWhiteSpace(model.AddressLine1) ||
                    string.IsNullOrWhiteSpace(model.City) ||
                    string.IsNullOrWhiteSpace(model.Province) ||
                    string.IsNullOrWhiteSpace(model.PostalCode))
                {
                    _logger.LogError("AddAddressAsync called with missing required fields");
                    return Guid.Empty;
                }

                // Validate user exists
                var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                _logger.LogInformation("User exists in database: {UserExists}", userExists);
                
                if (!userExists)
                {
                    _logger.LogError("User {UserId} does not exist in database", userId);
                    return Guid.Empty;
                }

                // Check if user has any existing addresses
                var existingAddressCount = await _context.Addresses
                    .CountAsync(a => a.UserId == userId);

                _logger.LogInformation("User {UserId} has {ExistingAddressCount} existing addresses", userId, existingAddressCount);

                // If this is the user's first address, automatically set it as default
                if (existingAddressCount == 0)
                {
                    model.IsDefault = true;
                    _logger.LogInformation("Setting first address as default for user {UserId}", userId);
                }

                // If this is set as default, remove default from other addresses
                if (model.IsDefault)
                {
                    _logger.LogInformation("Removing default from other addresses for user {UserId}", userId);
                    var defaultAddresses = await _context.Addresses
                        .Where(a => a.UserId == userId && a.IsDefault)
                        .ToListAsync();

                    foreach (var addr in defaultAddresses)
                    {
                        addr.IsDefault = false;
                        _logger.LogInformation("Removed default from address {PublicId}", addr.PublicId);
                    }
                }

                // Create new address - let database generate PublicId
                var address = new Address
                {
                    // Don't set PublicId - let database generate it with NEWID()
                    UserId = userId,
                    FullName = model.FullName.Trim(),
                    PhoneNumber = model.PhoneNumber?.Trim() ?? string.Empty,
                    AddressLine1 = model.AddressLine1.Trim(),
                    AddressLine2 = model.AddressLine2?.Trim(),
                    City = model.City.Trim(),
                    Province = model.Province.Trim(),
                    PostalCode = model.PostalCode.Trim(),
                    Country = model.Country?.Trim() ?? "South Africa",
                    IsDefault = model.IsDefault,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Created address object (PublicId will be generated by DB): {@Address}", new {
                    UserId = address.UserId,
                    FullName = address.FullName,
                    City = address.City,
                    Province = address.Province,
                    PostalCode = address.PostalCode,
                    Country = address.Country,
                    IsDefault = address.IsDefault
                });

                _context.Addresses.Add(address);
                _logger.LogInformation("Added address to context, attempting to save changes");
                
                var changesSaved = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync completed. Changes saved: {ChangesSaved}", changesSaved);

                if (changesSaved > 0)
                {
                    // Refresh the entity to get the database-generated PublicId
                    await _context.Entry(address).ReloadAsync();
                    
                    _logger.LogInformation("=== ADDRESS ADDED SUCCESSFULLY (Phase 3) ===");
                    _logger.LogInformation("User: {UserId}, PublicId: {PublicId}, IsDefault: {IsDefault}", 
                        userId, address.PublicId, address.IsDefault);
                    
                    return address.PublicId;
                }
                else
                {
                    _logger.LogError("=== NO CHANGES SAVED TO DATABASE ===");
                    _logger.LogError("This indicates a database constraint or validation issue");
                    
                    return Guid.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "=== EXCEPTION IN AddAddressAsync (Phase 3) ===");
                _logger.LogError("User: {UserId}", userId);
                _logger.LogError("Exception: {ExceptionMessage}", ex.Message);
                _logger.LogError("Stack trace: {StackTrace}", ex.StackTrace);
                
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {InnerExceptionMessage}", ex.InnerException.Message);
                    _logger.LogError("Inner stack trace: {InnerStackTrace}", ex.InnerException.StackTrace);
                }
                
                return Guid.Empty;
            }
        }

        public async Task<bool> UpdateAddressAsync(AddressViewModel model, string userId)
        {
            try
            {
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == model.Id && a.UserId == userId);

                if (address == null)
                {
                    _logger.LogWarning("Address {AddressId} not found for user {UserId}", model.Id, userId);
                    return false;
                }

                // If this is set as default, remove default from other addresses
                if (model.IsDefault && !address.IsDefault)
                {
                    await RemoveDefaultFromOtherAddressesAsync(userId, model.Id);
                }

                address.FullName = model.FullName;
                address.PhoneNumber = model.PhoneNumber;
                address.AddressLine1 = model.AddressLine1;
                address.AddressLine2 = model.AddressLine2;
                address.City = model.City;
                address.Province = model.Province;
                address.PostalCode = model.PostalCode;
                address.Country = model.Country;
                address.IsDefault = model.IsDefault;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Address {AddressId} updated successfully for user {UserId}", model.Id, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating address {AddressId} for user {UserId}", model.Id, userId);
                return false;
            }
        }

        public async Task<bool> DeleteAddressByPublicIdAsync(Guid publicId, string userId)
        {
            try
            {
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.PublicId == publicId && a.UserId == userId);

                if (address == null)
                {
                    _logger.LogWarning("Address {PublicId} not found for user {UserId}", publicId, userId);
                    return false;
                }

                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Address {PublicId} deleted successfully for user {UserId}", publicId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting address {PublicId} for user {UserId}", publicId, userId);
                return false;
            }
        }

        public async Task<bool> SetDefaultAddressByPublicIdAsync(Guid publicId, string userId)
        {
            try
            {
                var address = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.PublicId == publicId && a.UserId == userId);

                if (address == null)
                {
                    _logger.LogWarning("Address {PublicId} not found for user {UserId}", publicId, userId);
                    return false;
                }

                // Remove default from other addresses
                await RemoveDefaultFromOtherAddressesAsync(userId, address.Id);

                // Set this address as default
                address.IsDefault = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Address {PublicId} set as default for user {UserId}", publicId, userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default address {PublicId} for user {UserId}", publicId, userId);
                return false;
            }
        }

        public async Task<AddressViewModel> CreateAddressViewModelForUserAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                
                return new AddressViewModel
                {
                    FullName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : string.Empty,
                    Country = "South Africa" // Default country
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating address view model for user {UserId}", userId);
                return new AddressViewModel
                {
                    Country = "South Africa"
                };
            }
        }

        private async Task RemoveDefaultFromOtherAddressesAsync(string userId, int? excludeAddressId = null)
        {
            var query = _context.Addresses.Where(a => a.UserId == userId && a.IsDefault);
            
            if (excludeAddressId.HasValue)
            {
                query = query.Where(a => a.Id != excludeAddressId.Value);
            }

            var existingAddresses = await query.ToListAsync();
            
            foreach (var addr in existingAddresses)
            {
                addr.IsDefault = false;
            }
        }
    }
}