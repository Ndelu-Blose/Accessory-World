using AccessoryWorld.Data;
using AccessoryWorld.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccessoryWorld.Services
{
    public class ProductSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductSeeder> _logger;

        public ProductSeeder(ApplicationDbContext context, ILogger<ProductSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                if (!await _context.Categories.AnyAsync())
                {
                    await SeedCategoriesAsync();
                    await _context.SaveChangesAsync();
                }

                if (!await _context.Brands.AnyAsync())
                {
                    await SeedBrandsAsync();
                    await _context.SaveChangesAsync();
                }

                if (!await _context.Products.AnyAsync())
                {
                    await SeedProductsAsync();
                    await SeedSKUsAsync();
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Sample product data seeded successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding product data.");
                throw;
            }
        }

        private async Task SeedCategoriesAsync()
        {
            var categories = new[]
            {
                new Category { Name = "Smartphones", Description = "Mobile phones and accessories", IsActive = true },
                new Category { Name = "Tablets", Description = "Tablets and tablet accessories", IsActive = true },
                new Category { Name = "Laptops", Description = "Laptops and laptop accessories", IsActive = true },
                new Category { Name = "Audio", Description = "Headphones, speakers, and audio accessories", IsActive = true },
                new Category { Name = "Wearables", Description = "Smartwatches and fitness trackers", IsActive = true },
                new Category { Name = "Gaming", Description = "Gaming accessories and peripherals", IsActive = true },
                new Category { Name = "Cables & Chargers", Description = "Charging cables and power accessories", IsActive = true },
                new Category { Name = "Cases & Covers", Description = "Protective cases and covers", IsActive = true }
            };

            await _context.Categories.AddRangeAsync(categories);
        }

        private async Task SeedBrandsAsync()
        {
            var brands = new[]
            {
                new Brand { Name = "Apple", Description = "Premium technology products", IsActive = true },
                new Brand { Name = "Samsung", Description = "Innovative mobile and electronics", IsActive = true },
                new Brand { Name = "Huawei", Description = "Advanced telecommunications equipment", IsActive = true },
                new Brand { Name = "Sony", Description = "Entertainment and electronics", IsActive = true },
                new Brand { Name = "Anker", Description = "Charging and mobile accessories", IsActive = true },
                new Brand { Name = "JBL", Description = "Audio equipment and speakers", IsActive = true },
                new Brand { Name = "Logitech", Description = "Computer peripherals and accessories", IsActive = true },
                new Brand { Name = "Belkin", Description = "Connectivity and power solutions", IsActive = true }
            };

            await _context.Brands.AddRangeAsync(brands);
        }

        private async Task SeedProductsAsync()
        {
            // Get categories and brands
            var smartphones = await _context.Categories.FirstAsync(c => c.Name == "Smartphones");
            var audio = await _context.Categories.FirstAsync(c => c.Name == "Audio");
            var cables = await _context.Categories.FirstAsync(c => c.Name == "Cables & Chargers");
            var cases = await _context.Categories.FirstAsync(c => c.Name == "Cases & Covers");
            var wearables = await _context.Categories.FirstAsync(c => c.Name == "Wearables");

            var apple = await _context.Brands.FirstAsync(b => b.Name == "Apple");
            var samsung = await _context.Brands.FirstAsync(b => b.Name == "Samsung");
            var anker = await _context.Brands.FirstAsync(b => b.Name == "Anker");
            var jbl = await _context.Brands.FirstAsync(b => b.Name == "JBL");

            var products = new[]
            {
                // Smartphones
                new Product
                {
                    Name = "iPhone 15 Pro",
                    Description = "The most advanced iPhone yet with titanium design, A17 Pro chip, and pro camera system.",
                    Price = 24999.00m,
                    CompareAtPrice = 26999.00m,
                    IsOnSale = true,
                    SalePrice = 22999.00m,
                    CategoryId = smartphones.Id,
                    BrandId = apple.Id,
                    IsActive = true,
                    IsNew = true,
                    IsHot = true,
                    IsTodayDeal = false,
                    IsBestSeller = true,
                    InStock = true,
                    SalesCount = 1250,
                    Condition = "New",
                    Tags = "flagship,premium,titanium,pro",
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://via.placeholder.com/500x500?text=iPhone+15+Pro", IsPrimary = true, SortOrder = 1 }
                    },
                    ProductSpecifications = new List<ProductSpecification>
                    {
                        new ProductSpecification { SpecificationName = "Display", SpecificationValue = "6.1-inch Super Retina XDR" },
                        new ProductSpecification { SpecificationName = "Chip", SpecificationValue = "A17 Pro" },
                        new ProductSpecification { SpecificationName = "Storage", SpecificationValue = "128GB" },
                        new ProductSpecification { SpecificationName = "Camera", SpecificationValue = "48MP Main + 12MP Ultra Wide + 12MP Telephoto" }
                    }
                },
                new Product
                {
                    Name = "Samsung Galaxy S24 Ultra",
                    Description = "Ultimate Galaxy experience with S Pen, 200MP camera, and AI-powered features.",
                    Price = 26999.00m,
                    CompareAtPrice = 28999.00m,
                    CategoryId = smartphones.Id,
                    BrandId = samsung.Id,
                    IsActive = true,
                    IsNew = true,
                    IsHot = false,
                    IsTodayDeal = true,
                    IsBestSeller = true,
                    InStock = true,
                    SalesCount = 980,
                    Condition = "New",
                    Tags = "flagship,s-pen,ai,camera",
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://via.placeholder.com/500x500?text=Galaxy+S24+Ultra", IsPrimary = true, SortOrder = 1 }
                    },
                    ProductSpecifications = new List<ProductSpecification>
                    {
                        new ProductSpecification { SpecificationName = "Display", SpecificationValue = "6.8-inch Dynamic AMOLED 2X" },
                        new ProductSpecification { SpecificationName = "Processor", SpecificationValue = "Snapdragon 8 Gen 3" },
                        new ProductSpecification { SpecificationName = "Storage", SpecificationValue = "256GB" },
                        new ProductSpecification { SpecificationName = "Camera", SpecificationValue = "200MP Main + 50MP Periscope + 12MP Ultra Wide + 10MP Telephoto" }
                    }
                },
                
                // Audio
                new Product
                {
                    Name = "AirPods Pro (2nd Generation)",
                    Description = "Active Noise Cancellation, Transparency mode, and Personalized Spatial Audio.",
                    Price = 4999.00m,
                    CompareAtPrice = 5499.00m,
                    IsOnSale = true,
                    SalePrice = 4499.00m,
                    CategoryId = audio.Id,
                    BrandId = apple.Id,
                    IsActive = true,
                    IsNew = false,
                    IsHot = true,
                    IsTodayDeal = true,
                    IsBestSeller = true,
                    InStock = true,
                    SalesCount = 2150,
                    Condition = "New",
                    Tags = "wireless,anc,spatial-audio",
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://via.placeholder.com/500x500?text=AirPods+Pro", IsPrimary = true, SortOrder = 1 }
                    },
                    ProductSpecifications = new List<ProductSpecification>
                    {
                        new ProductSpecification { SpecificationName = "Battery Life", SpecificationValue = "Up to 6 hours listening time" },
                        new ProductSpecification { SpecificationName = "Connectivity", SpecificationValue = "Bluetooth 5.3" },
                        new ProductSpecification { SpecificationName = "Features", SpecificationValue = "Active Noise Cancellation, Transparency mode" }
                    }
                },
                new Product
                {
                    Name = "JBL Charge 5",
                    Description = "Portable Bluetooth speaker with powerful sound and built-in powerbank.",
                    Price = 2999.00m,
                    CompareAtPrice = 3499.00m,
                    CategoryId = audio.Id,
                    BrandId = jbl.Id,
                    IsActive = true,
                    IsNew = false,
                    IsHot = false,
                    IsTodayDeal = false,
                    IsBestSeller = true,
                    InStock = true,
                    SalesCount = 750,
                    Condition = "New",
                    Tags = "bluetooth,speaker,waterproof,powerbank",
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://via.placeholder.com/500x500?text=JBL+Charge+5", IsPrimary = true, SortOrder = 1 }
                    },
                    ProductSpecifications = new List<ProductSpecification>
                    {
                        new ProductSpecification { SpecificationName = "Battery Life", SpecificationValue = "Up to 20 hours" },
                        new ProductSpecification { SpecificationName = "Water Resistance", SpecificationValue = "IP67" },
                        new ProductSpecification { SpecificationName = "Power Output", SpecificationValue = "40W" }
                    }
                },
                
                // Cables & Chargers
                new Product
                {
                    Name = "Anker PowerCore 10000",
                    Description = "Ultra-compact portable charger with high-speed charging technology.",
                    Price = 799.00m,
                    CompareAtPrice = 999.00m,
                    CategoryId = cables.Id,
                    BrandId = anker.Id,
                    IsActive = true,
                    IsNew = false,
                    IsHot = true,
                    IsTodayDeal = false,
                    IsBestSeller = true,
                    InStock = true,
                    SalesCount = 1850,
                    Condition = "New",
                    Tags = "powerbank,portable,fast-charging",
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://via.placeholder.com/500x500?text=Anker+PowerCore", IsPrimary = true, SortOrder = 1 }
                    },
                    ProductSpecifications = new List<ProductSpecification>
                    {
                        new ProductSpecification { SpecificationName = "Capacity", SpecificationValue = "10000mAh" },
                        new ProductSpecification { SpecificationName = "Output", SpecificationValue = "12W" },
                        new ProductSpecification { SpecificationName = "Weight", SpecificationValue = "180g" }
                    }
                },
                new Product
                {
                    Name = "USB-C to Lightning Cable",
                    Description = "Fast charging cable for iPhone and iPad with USB-C connector.",
                    Price = 299.00m,
                    CompareAtPrice = 399.00m,
                    CategoryId = cables.Id,
                    BrandId = apple.Id,
                    IsActive = true,
                    IsNew = false,
                    IsHot = false,
                    IsTodayDeal = true,
                    IsBestSeller = false,
                    InStock = true,
                    SalesCount = 3200,
                    Condition = "New",
                    Tags = "cable,usb-c,lightning,fast-charging",
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://via.placeholder.com/500x500?text=USB-C+Cable", IsPrimary = true, SortOrder = 1 }
                    },
                    ProductSpecifications = new List<ProductSpecification>
                    {
                        new ProductSpecification { SpecificationName = "Length", SpecificationValue = "1 meter" },
                        new ProductSpecification { SpecificationName = "Compatibility", SpecificationValue = "iPhone 15 series, iPad" },
                        new ProductSpecification { SpecificationName = "Data Transfer", SpecificationValue = "USB 2.0" }
                    }
                },
                
                // Wearables
                new Product
                {
                    Name = "Apple Watch Series 9",
                    Description = "Advanced health monitoring, fitness tracking, and smart features.",
                    Price = 8999.00m,
                    CategoryId = wearables.Id,
                    BrandId = apple.Id,
                    IsActive = true,
                    ProductImages = new List<ProductImage>
                    {
                        new ProductImage { ImageUrl = "https://via.placeholder.com/500x500?text=Apple+Watch+S9", IsPrimary = true, SortOrder = 1 }
                    },
                    ProductSpecifications = new List<ProductSpecification>
                    {
                        new ProductSpecification { SpecificationName = "Display", SpecificationValue = "45mm Retina LTPO OLED" },
                        new ProductSpecification { SpecificationName = "Chip", SpecificationValue = "S9 SiP" },
                        new ProductSpecification { SpecificationName = "Battery Life", SpecificationValue = "Up to 18 hours" },
                        new ProductSpecification { SpecificationName = "Water Resistance", SpecificationValue = "50 meters" }
                    }
                }
            };

            await _context.Products.AddRangeAsync(products);
        }

        private async Task SeedSKUsAsync()
        {
            // Wait for products to be saved first
            await _context.SaveChangesAsync();

            var products = await _context.Products.ToListAsync();
            var skus = new List<SKU>();

            foreach (var product in products)
            {
                // Create a default SKU for each product
                skus.Add(new SKU
                {
                    ProductId = product.Id,
                    SKUCode = $"SKU-{product.Id:D6}",
                    Price = product.Price,
                    StockQuantity = Random.Shared.Next(5, 50),
                    LowStockThreshold = 5,
                    IsActive = true
                });
            }

            await _context.SKUs.AddRangeAsync(skus);
        }
    }
}