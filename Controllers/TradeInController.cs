using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AccessoryWorld.Services;
using AccessoryWorld.Models;
using AccessoryWorld.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;

namespace AccessoryWorld.Controllers
{
    [Authorize]
    public class TradeInController : Controller
    {
        private readonly ITradeInService _tradeInService;
        private readonly ILogger<TradeInController> _logger;
        private readonly IWebHostEnvironment _env;

        public TradeInController(ITradeInService tradeInService, ILogger<TradeInController> logger, IWebHostEnvironment env)
        {
            _tradeInService = tradeInService;
            _logger = logger;
            _env = env;
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
            return View(new AccessoryWorld.Models.ViewModels.CreateTradeInRequest
            {
                FullName = "",
                Email = "",
                Phone = "",
                DeviceBrand = "",
                DeviceModel = "",
                DeviceType = "",
                Description = ""
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Consumes("multipart/form-data")]
        [RequestFormLimits(MultipartBodyLengthLimit = 256 * 1024 * 1024)]
        public async Task<IActionResult> Create([FromForm] AccessoryWorld.Models.ViewModels.CreateTradeInRequest vm, CancellationToken ct)
        {
            // Enhanced logging for Phase 4 - Log all incoming request details
            _logger.LogInformation("TradeIn Create POST - Request received");
            _logger.LogInformation("TradeIn Create - Content-Type: {ContentType}, Content-Length: {ContentLength}", 
                Request.ContentType, Request.ContentLength);
            _logger.LogInformation("TradeIn Create - User: {UserId}, IP: {RemoteIpAddress}", 
                User.FindFirstValue(ClaimTypes.NameIdentifier), HttpContext.Connection.RemoteIpAddress);
            
            // Log form data details (excluding sensitive information)
            _logger.LogInformation("TradeIn Create - Form Data: DeviceBrand={DeviceBrand}, DeviceModel={DeviceModel}, DeviceType={DeviceType}, ConditionGrade={ConditionGrade}, ProposedValue={ProposedValue}, Description Length={DescriptionLength}",
                vm.DeviceBrand, vm.DeviceModel, vm.DeviceType, vm.ConditionGrade, vm.ProposedValue, vm.Description?.Length ?? 0);
            
            // Log file upload details
            if (vm.Photos != null)
            {
                _logger.LogInformation("TradeIn Create - Photos uploaded: {PhotoCount}", vm.Photos.Count());
                foreach (var photo in vm.Photos.Where(p => p != null))
                {
                    _logger.LogInformation("TradeIn Create - Photo: {FileName}, Size: {Length} bytes, ContentType: {ContentType}",
                        photo.FileName, photo.Length, photo.ContentType);
                }
            }
            
            if (!ModelState.IsValid)
            {
                // Enhanced model state error logging
                _logger.LogWarning("TradeIn Create - ModelState is invalid. Total errors: {ErrorCount}", ModelState.ErrorCount);
                foreach (var kvp in ModelState)
                {
                    if (kvp.Value.Errors.Any())
                    {
                        foreach (var err in kvp.Value.Errors)
                        {
                            _logger.LogWarning("TradeIn Create ModelState error for {Field}: {Error} (AttemptedValue: {AttemptedValue})", 
                                kvp.Key, err.ErrorMessage, kvp.Value.AttemptedValue);
                        }
                    }
                }

                // IMPORTANT: return the same view with the same model (do NOT redirect)
                _logger.LogInformation("TradeIn Create - Returning view with validation errors");
                return View("Create", vm);
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("TradeIn Create - User not authenticated, redirecting to login");
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("TradeIn Create - Processing valid submission for user {UserId}", userId);

                // Save photos if any
                var savedPhotos = new List<string>();
                if (vm.Photos != null && vm.Photos.Any(f => f != null && f.Length > 0))
                {
                    _logger.LogInformation("TradeIn Create - Processing {PhotoCount} photo uploads", vm.Photos.Count(f => f != null && f.Length > 0));
                    
                    // Make sure we always have a web root to write into
                    var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
                    var baseDir = Path.Combine(webRoot, "uploads", "tradeins", DateTime.UtcNow.ToString("yyyyMMdd"));
                    Directory.CreateDirectory(baseDir);
                    
                    _logger.LogInformation("TradeIn Create - Saving photos to directory: {Directory}", baseDir);

                    foreach (var photo in vm.Photos.Where(f => f != null && f.Length > 0))
                    {
                        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}";
                        var fullPath = Path.Combine(baseDir, fileName);
                        await using var stream = System.IO.File.Create(fullPath);
                        await photo.CopyToAsync(stream, ct);
                        var relativePath = $"/uploads/tradeins/{DateTime.UtcNow:yyyyMMdd}/{fileName}";
                        savedPhotos.Add(relativePath);
                        
                        _logger.LogInformation("TradeIn Create - Photo saved: {OriginalName} -> {SavedPath}", photo.FileName, relativePath);
                    }
                }

                // Map to a *differently named* service command to avoid type-name collisions
                var serviceRequest = new AccessoryWorld.Services.CreateTradeInRequest
                {
                    CustomerId    = userId,
                    DeviceBrand   = vm.DeviceBrand,
                    DeviceModel   = vm.DeviceModel,
                    DeviceType    = vm.DeviceType,
                    Description   = vm.Description,
                    Photos        = savedPhotos,
                    // Include these if your service accepts them:
                    ConditionGrade = vm.ConditionGrade,
                    ProposedValue  = vm.ProposedValue
                };

                _logger.LogInformation("TradeIn Create - Calling service with request: {ServiceRequest}", 
                    System.Text.Json.JsonSerializer.Serialize(serviceRequest, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

                var tradeIn = await _tradeInService.CreateTradeInAsync(serviceRequest);

                _logger.LogInformation("Trade-in submitted successfully by {UserId} for {Brand} {Model} - TradeIn ID: {TradeInId}", 
                    userId, vm.DeviceBrand, vm.DeviceModel, tradeIn?.Id);
                TempData["SuccessMessage"] = $"Trade-in request submitted successfully! Your case number is: {tradeIn.PublicId}";

                // PRG pattern only on success
                return RedirectToAction("Details", new { id = tradeIn.PublicId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating trade-in request");
                ModelState.AddModelError(string.Empty, "We couldn't submit your trade-in request. Please try again.");
                return View("Create", vm);
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