using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.Models;

namespace AccessoryWorld.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public List<Product> BestSellers { get; set; } = new();
        public List<Product> Laptops { get; set; } = new();
        public List<Product> IwatchProducts { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Get products with their related data
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.SKUs)
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive)
                .ToListAsync();

            // Best Sellers - products with highest view count or random selection
            BestSellers = products
                .OrderBy(x => Guid.NewGuid())
                .Take(8)
                .ToList();

            // Laptops category
            Laptops = products
                .Where(p => p.Category != null && p.Category.Name.ToLower().Contains("laptop"))
                .Take(8)
                .ToList();

            // Iwatch/Smartwatch category
            IwatchProducts = products
                .Where(p => p.Category != null && 
                       (p.Category.Name.ToLower().Contains("watch") || 
                        p.Category.Name.ToLower().Contains("smart") ||
                        p.Name.ToLower().Contains("watch")))
                .Take(8)
                .ToList();

            // Featured products for other sections
            FeaturedProducts = products
                .OrderBy(x => Guid.NewGuid())
                .Take(4)
                .ToList();
        }
    }
}
