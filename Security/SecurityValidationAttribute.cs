using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AccessoryWorld.Services;

namespace AccessoryWorld.Security
{
    public class SecurityValidationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var securityService = context.HttpContext.RequestServices
                .GetService<ISecurityValidationService>();
            
            if (securityService == null)
            {
                base.OnActionExecuting(context);
                return;
            }
            
            // Validate string parameters for XSS and SQL injection
            foreach (var param in context.ActionArguments)
            {
                if (param.Value is string stringValue && !string.IsNullOrEmpty(stringValue))
                {
                    if (securityService.ContainsXssPatterns(stringValue) || 
                        securityService.ContainsSqlInjectionPatterns(stringValue))
                    {
                        context.Result = new BadRequestObjectResult(new 
                        { 
                            error = "Invalid input detected",
                            message = "The request contains potentially dangerous content."
                        });
                        return;
                    }
                }
            }
            
            // Validate file uploads if present
            var files = context.HttpContext.Request.Form.Files;
            if (files.Any())
            {
                foreach (var file in files)
                {
                    if (!ValidateFileUpload(file, securityService))
                    {
                        context.Result = new BadRequestObjectResult(new 
                        { 
                            error = "Invalid file upload",
                            message = "The uploaded file does not meet security requirements."
                        });
                        return;
                    }
                }
            }
            
            base.OnActionExecuting(context);
        }
        
        private static bool ValidateFileUpload(IFormFile file, ISecurityValidationService securityService)
        {
            // Define allowed file types and max size based on context
            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var allowedDocumentExtensions = new[] { ".pdf", ".doc", ".docx", ".txt" };
            var maxImageSize = 5 * 1024 * 1024; // 5MB
            var maxDocumentSize = 10 * 1024 * 1024; // 10MB
            
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            
            // Check if it's an image file
            if (allowedImageExtensions.Contains(extension))
            {
                return securityService.IsValidFileUpload(file, allowedImageExtensions, maxImageSize);
            }
            
            // Check if it's a document file
            if (allowedDocumentExtensions.Contains(extension))
            {
                return securityService.IsValidFileUpload(file, allowedDocumentExtensions, maxDocumentSize);
            }
            
            // File type not allowed
            return false;
        }
    }
    
    public class ValidateFileUploadAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedExtensions;
        private readonly long _maxSizeBytes;
        
        public ValidateFileUploadAttribute(string[] allowedExtensions, long maxSizeBytes)
        {
            _allowedExtensions = allowedExtensions;
            _maxSizeBytes = maxSizeBytes;
        }
        
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var securityService = context.HttpContext.RequestServices
                .GetService<ISecurityValidationService>();
            
            if (securityService == null)
            {
                context.Result = new StatusCodeResult(500);
                return;
            }
            
            var files = context.HttpContext.Request.Form.Files;
            
            foreach (var file in files)
            {
                if (!securityService.IsValidFileUpload(file, _allowedExtensions, _maxSizeBytes))
                {
                    context.Result = new BadRequestObjectResult(new 
                    { 
                        error = "Invalid file upload",
                        message = $"File must be one of: {string.Join(", ", _allowedExtensions)} and under {_maxSizeBytes / (1024 * 1024)}MB"
                    });
                    return;
                }
            }
            
            base.OnActionExecuting(context);
        }
    }
}