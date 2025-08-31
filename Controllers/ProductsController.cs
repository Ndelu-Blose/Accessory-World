using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.ViewModels;
using AccessoryWorld.Security;
using AccessoryWorld.Services;

namespace AccessoryWorld.Controllers
{
    public class ProductsController(ApplicationDbContext context, ISecurityValidationService securityValidation) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ISecurityValidationService _securityValidation = securityValidation;

        // GET: Products
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Index(
            string? searchTerm,
            int? categoryId,
            int? brandId,
            string? sortBy = "name",
            string? sortOrder = "asc",
            string? brand = null,
            string? condition = null,
            bool? inStock = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            bool? isOnSale = null,
            bool? isBestSeller = null,
            bool? isNew = null,
            bool? isHot = null,
            bool? isTodayDeal = null,
            string? viewMode = "grid",
            int page = 1,
            int pageSize = 12)
        {
            // Create ShopVM instance
            var shopVM = new ShopVM
            {
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                BrandId = brandId,
                SortBy = sortBy,
                SortOrder = sortOrder,
                Condition = condition,
                InStock = inStock,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                IsOnSale = isOnSale,
                IsBestSeller = isBestSeller,
                IsNew = isNew,
                IsHot = isHot,
                IsTodayDeal = isTodayDeal,
                ViewMode = viewMode,
                CurrentPage = page,
                PageSize = pageSize
            };

            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.SKUs)
                .Include(p => p.ProductImages)
                .Where(p => p.IsActive);

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                // Sanitize search input to prevent XSS and SQL injection
                if (_securityValidation.ContainsXssPatterns(searchTerm) || _securityValidation.ContainsSqlInjectionPatterns(searchTerm))
                {
                    TempData["Error"] = "Invalid search terms detected.";
                    return RedirectToAction(nameof(Index));
                }
                
                var sanitizedSearch = _securityValidation.SanitizeHtml(searchTerm);
                query = query.Where(p => p.Name.Contains(sanitizedSearch) || 
                                        p.Description!.Contains(sanitizedSearch) ||
                                        p.Brand.Name.Contains(sanitizedSearch) ||
                                        p.Category.Name.Contains(sanitizedSearch));
            }

            // Apply category filter
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            // Apply brand filter
            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }
            
            // Apply brand name filter (for dropdown)
            if (!string.IsNullOrEmpty(brand))
            {
                query = query.Where(p => p.Brand.Name.ToLower() == brand.ToLower());
            }
            
            // Apply condition filter
            if (!string.IsNullOrEmpty(condition))
            {
                query = query.Where(p => p.Condition != null && p.Condition.ToLower() == condition.ToLower());
            }

            // Apply stock filter
            if (inStock.HasValue && inStock.Value)
            {
                query = query.Where(p => p.InStock);
            }
            
            // Apply price range filter
            if (minPrice.HasValue)
            {
                query = query.Where(p => (p.IsOnSale ? p.SalePrice ?? p.Price : p.Price) >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => (p.IsOnSale ? p.SalePrice ?? p.Price : p.Price) <= maxPrice.Value);
            }
            
            // Apply feature filters
            if (isOnSale.HasValue && isOnSale.Value)
            {
                query = query.Where(p => p.IsOnSale);
            }
            if (isBestSeller.HasValue && isBestSeller.Value)
            {
                query = query.Where(p => p.IsBestSeller);
            }
            if (isNew.HasValue && isNew.Value)
            {
                query = query.Where(p => p.IsNew);
            }
            if (isHot.HasValue && isHot.Value)
            {
                query = query.Where(p => p.IsHot);
            }
            if (isTodayDeal.HasValue && isTodayDeal.Value)
            {
                query = query.Where(p => p.IsTodayDeal);
            }

            // Apply sorting
            var sortKey = $"{sortBy}_{sortOrder}".ToLower();
            query = sortKey switch
            {
                "price_asc" => query.OrderBy(p => p.IsOnSale ? p.SalePrice ?? p.Price : p.Price),
                "price_desc" => query.OrderByDescending(p => p.IsOnSale ? p.SalePrice ?? p.Price : p.Price),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "bestseller" => query.OrderByDescending(p => p.IsBestSeller).ThenByDescending(p => p.SalesCount),
                "sales_desc" => query.OrderByDescending(p => p.SalesCount),
                "name_desc" => query.OrderByDescending(p => p.Name),
                "name_asc" or _ => query.OrderBy(p => p.Name)
            };

            // Get total count for pagination
            shopVM.TotalProducts = await query.CountAsync();

            // Apply pagination
            shopVM.Products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get filter data
            shopVM.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            shopVM.Brands = await _context.Brands.OrderBy(b => b.Name).ToListAsync();

            return View("~/Views/Customer/Products/Index.cshtml", shopVM);
        }

        // GET: Products/Details/5
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductSpecifications)
                .Include(p => p.SKUs)
                    .ThenInclude(s => s.StockMovements)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            // Get related products
            var relatedProducts = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View("~/Views/Customer/Products/Details.cshtml", product);
    }
    
    // API endpoint to get default SKU for a product
    [HttpGet("api/products/{productId}/default-sku")]
    public async Task<IActionResult> GetDefaultSku(int productId)
    {
        var sku = await _context.SKUs
            .Where(s => s.ProductId == productId && s.IsActive)
            .OrderBy(s => s.Id) // Get the first/default SKU
            .FirstOrDefaultAsync();
            
        if (sku == null)
        {
            return NotFound(new { message = "No SKU found for this product" });
        }
        
        return Json(new { skuId = sku.Id, skuCode = sku.SKUCode, stockQuantity = sku.AvailableQuantity });
    }
}
}