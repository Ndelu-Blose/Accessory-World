using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace AccessoryWorld.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsletterController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NewsletterController> _logger;

        public NewsletterController(ApplicationDbContext context, ILogger<NewsletterController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromBody] NewsletterSubscriptionRequest request)
        {
            try
            {
                // Validate email format
                if (!IsValidEmail(request.Email))
                {
                    return BadRequest(new { success = false, message = "Please enter a valid email address." });
                }

                // Check if email already exists and is active
                var existingSubscription = await _context.Newsletters
                    .FirstOrDefaultAsync(n => n.Email.ToLower() == request.Email.ToLower());

                if (existingSubscription != null)
                {
                    if (existingSubscription.IsActive)
                    {
                        return Ok(new { success = true, message = "You're already subscribed to our newsletter!" });
                    }
                    else
                    {
                        // Reactivate subscription
                        existingSubscription.IsActive = true;
                        existingSubscription.SubscribedAt = DateTime.UtcNow;
                        existingSubscription.UnsubscribedAt = null;
                        existingSubscription.IpAddress = GetClientIpAddress();
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation($"Newsletter subscription reactivated for email: {request.Email}");
                        return Ok(new { success = true, message = "Welcome back! You've been subscribed to our newsletter." });
                    }
                }

                // Create new subscription
                var newsletter = new Newsletter
                {
                    Email = request.Email.ToLower().Trim(),
                    SubscribedAt = DateTime.UtcNow,
                    IsActive = true,
                    Source = "landing_page",
                    IpAddress = GetClientIpAddress()
                };

                _context.Newsletters.Add(newsletter);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New newsletter subscription created for email: {request.Email}");
                
                return Ok(new { success = true, message = "Thank you for subscribing to our newsletter!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error subscribing email {request.Email} to newsletter");
                return StatusCode(500, new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromBody] NewsletterUnsubscribeRequest request)
        {
            try
            {
                var subscription = await _context.Newsletters
                    .FirstOrDefaultAsync(n => n.Email.ToLower() == request.Email.ToLower() && n.IsActive);

                if (subscription == null)
                {
                    return NotFound(new { success = false, message = "Email not found in our newsletter list." });
                }

                subscription.IsActive = false;
                subscription.UnsubscribedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                _logger.LogInformation($"Newsletter unsubscription processed for email: {request.Email}");
                
                return Ok(new { success = true, message = "You have been successfully unsubscribed from our newsletter." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error unsubscribing email {request.Email} from newsletter");
                return StatusCode(500, new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailAttribute = new EmailAddressAttribute();
                return emailAttribute.IsValid(email);
            }
            catch
            {
                return false;
            }
        }

        private string? GetClientIpAddress()
        {
            try
            {
                // Try to get the real IP address from headers (in case of proxy/load balancer)
                var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(xForwardedFor))
                {
                    return xForwardedFor.Split(',')[0].Trim();
                }

                var xRealIp = Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(xRealIp))
                {
                    return xRealIp;
                }

                // Fallback to connection remote IP
                return HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }

    public class NewsletterSubscriptionRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class NewsletterUnsubscribeRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}