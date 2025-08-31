using AccessoryWorld.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AccessoryWorld.Controllers
{
    [Route("[controller]")]
    public class WishlistController : Controller
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        // GET: /Wishlist
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var wishlistItems = await _wishlistService.GetWishlistItemsAsync(userId);
            return View(wishlistItems);
        }

        // POST: /Wishlist/Add
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Add(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Please log in to add items to wishlist." });

            var success = await _wishlistService.AddToWishlistAsync(userId, productId);
            
            if (success)
            {
                var count = await _wishlistService.GetWishlistCountAsync(userId);
                return Json(new { success = true, message = "Item added to wishlist!", count = count });
            }
            
            return Json(new { success = false, message = "Failed to add item to wishlist." });
        }

        // POST: /Wishlist/Remove
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Remove(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Please log in." });

            var success = await _wishlistService.RemoveFromWishlistAsync(userId, productId);
            
            if (success)
            {
                var count = await _wishlistService.GetWishlistCountAsync(userId);
                return Json(new { success = true, message = "Item removed from wishlist!", count = count });
            }
            
            return Json(new { success = false, message = "Failed to remove item from wishlist." });
        }

        // GET: /Wishlist/Test
        [HttpGet("Test")]
        public IActionResult Test()
        {
            return Json(new { message = "Wishlist controller is working", timestamp = DateTime.Now });
        }

        // GET: /Wishlist/Count
        [HttpGet("Count")]
        public async Task<IActionResult> Count()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !User.Identity.IsAuthenticated)
                return Json(new { count = 0 });

            var count = await _wishlistService.GetWishlistCountAsync(userId);
            return Json(new { count = count });
        }

        // GET: /Wishlist/IsInWishlist
        [HttpGet("IsInWishlist")]
        public async Task<IActionResult> IsInWishlist(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId) || !User.Identity.IsAuthenticated)
                return Json(new { isInWishlist = false });

            var isInWishlist = await _wishlistService.IsInWishlistAsync(userId, productId);
            return Json(new { isInWishlist = isInWishlist });
        }

        // POST: /Wishlist/Clear
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Clear()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Please log in." });

            var success = await _wishlistService.ClearWishlistAsync(userId);
            
            if (success)
            {
                return Json(new { success = true, message = "Wishlist cleared successfully!" });
            }
            
            return Json(new { success = false, message = "Failed to clear wishlist." });
        }
    }
}