using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.ViewModels;

namespace AccessoryWorld.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Create a basic ShopVM for the home page with featured products
            var shopVM = new ShopVM
            {
                ViewMode = "grid",
                CurrentPage = 1,
                PageSize = 12
            };

            // Get featured products (hot, new, on sale, best sellers)
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.SKUs)
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive && (p.IsHot || p.IsNew || p.IsOnSale || p.IsBestSeller || p.IsTodayDeal))
                .OrderByDescending(p => p.IsTodayDeal)
                .ThenByDescending(p => p.IsHot)
                .ThenByDescending(p => p.IsNew)
                .ThenByDescending(p => p.IsBestSeller)
                .ThenByDescending(p => p.IsOnSale);

            shopVM.TotalProducts = await query.CountAsync();
            shopVM.Products = await query.Take(12).ToListAsync();

            // Get filter data
            shopVM.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            shopVM.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();

            return View("~/Views/Customer/Products/Index.cshtml", shopVM);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}