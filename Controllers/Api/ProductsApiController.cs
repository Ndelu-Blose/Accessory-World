using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;

namespace AccessoryWorld.Controllers.Api
{
    [ApiController]
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProductsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("{productId}/default-sku")]
        public async Task<IActionResult> GetDefaultSku(int productId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.SKUs)
                    .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                var defaultSku = product.SKUs
                    .Where(s => s.StockQuantity > s.ReservedQuantity)
                    .OrderBy(s => s.Id)
                    .FirstOrDefault();

                if (defaultSku == null)
                {
                    return NotFound(new { message = "No available SKU found for this product" });
                }

                return Ok(new 
                {
                    skuId = defaultSku.Id,
                    variant = defaultSku.Variant ?? "Standard",
                    availableStock = defaultSku.StockQuantity - defaultSku.ReservedQuantity,
                    price = product.IsOnSale && product.SalePrice.HasValue ? product.SalePrice.Value : product.Price
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving product SKU" });
            }
        }
    }
}