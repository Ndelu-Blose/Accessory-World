using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AccessoryWorld.Services;
using AccessoryWorld.Models;
using System.Security.Claims;

namespace AccessoryWorld.Controllers
{
    [Authorize]
    public class TradeInController : Controller
    {
        private readonly ITradeInService _tradeInService;
        private readonly ILogger<TradeInController> _logger;

        public TradeInController(ITradeInService tradeInService, ILogger<TradeInController> logger)
        {
            _tradeInService = tradeInService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var userTradeIns = await _tradeInService.GetUserTradeInsAsync(userId);
            return View(userTradeIns);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccessoryWorld.Models.CreateTradeInRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return RedirectToAction("Login", "Account");

                request.CustomerId = userId;
                
                // Convert to service model
                var serviceRequest = new AccessoryWorld.Services.CreateTradeInRequest
                {
                    CustomerId = request.CustomerId,
                    DeviceBrand = request.DeviceBrand,
                    DeviceModel = request.DeviceModel,
                    IMEI = request.IMEI,
                    ConditionGrade = request.ConditionGrade,
                    Photos = request.Photos,
                    ProposedValue = request.ProposedValue
                };
                
                var tradeIn = await _tradeInService.CreateTradeInAsync(serviceRequest);
                
                TempData["SuccessMessage"] = $"Trade-in request submitted successfully! Your case number is: {tradeIn.PublicId}";
                return RedirectToAction("Details", new { id = tradeIn.PublicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trade-in request");
                ModelState.AddModelError("", "An error occurred while submitting your trade-in request. Please try again.");
                return View(request);
            }
        }

        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var tradeIn = await _tradeInService.GetTradeInByPublicIdAsync(id);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Users can only see their own trade-ins
                if (tradeIn.CustomerId != userId)
                    return Forbid();

                return View(tradeIn);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving trade-in details");
                return NotFound();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(Guid id)
        {
            try
            {
                var tradeIn = await _tradeInService.GetTradeInByPublicIdAsync(id);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Users can only accept their own trade-ins
                if (tradeIn.CustomerId != userId)
                    return Forbid();

                var creditNote = await _tradeInService.AcceptTradeInAsync(tradeIn.Id);
                TempData["SuccessMessage"] = $"Trade-in offer accepted! Credit note {creditNote.CreditNoteCode} has been issued to your account.";
                
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting trade-in offer");
                TempData["ErrorMessage"] = "An error occurred while accepting the trade-in offer. Please try again.";
                return RedirectToAction("Details", new { id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id)
        {
            try
            {
                var tradeIn = await _tradeInService.GetTradeInByPublicIdAsync(id);
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Users can only reject their own trade-ins
                if (tradeIn.CustomerId != userId)
                    return Forbid();

                await _tradeInService.UpdateTradeInStatusAsync(tradeIn.Id, "REJECTED");
                TempData["InfoMessage"] = "Trade-in offer has been rejected.";
                
                return RedirectToAction("Details", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting trade-in offer");
                TempData["ErrorMessage"] = "An error occurred while rejecting the trade-in offer. Please try again.";
                return RedirectToAction("Details", new { id });
            }
        }
    }
}