using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using System.Web;

namespace AccessoryWorld.Services
{
    public interface ISecurityValidationService
    {
        string SanitizeHtml(string input);
        bool IsValidFileUpload(IFormFile file, string[] allowedExtensions, long maxSizeBytes);
        bool IsValidImageFile(IFormFile file);
        string GenerateSecureFileName(string originalFileName);
        bool IsValidUrl(string url);
        bool ContainsSqlInjectionPatterns(string input);
        bool ContainsXssPatterns(string input);
        string EscapeForHtml(string input);
        bool IsValidPhoneNumber(string phoneNumber);
        bool IsValidSouthAfricanId(string idNumber);
    }
    
    public class SecurityValidationService : ISecurityValidationService
    {
        private readonly ILogger<SecurityValidationService> _logger;
        private readonly string[] _dangerousPatterns = {
            "<script", "javascript:", "vbscript:", "onload=", "onerror=",
            "onclick=", "onmouseover=", "onfocus=", "onblur=", "onchange=",
            "onsubmit=", "onreset=", "onselect=", "onunload="
        };
        
        private readonly string[] _sqlInjectionPatterns = {
            "'", "--", "/*", "*/", "xp_", "sp_", "union", "select",
            "insert", "delete", "update", "drop", "create", "alter",
            "exec", "execute", "declare", "cast", "convert"
        };
        
        public SecurityValidationService(ILogger<SecurityValidationService> logger)
        {
            _logger = logger;
        }
        
        public string SanitizeHtml(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;
            
            // Remove dangerous patterns
            var sanitized = input;
            foreach (var pattern in _dangerousPatterns)
            {
                sanitized = Regex.Replace(sanitized, pattern, "", RegexOptions.IgnoreCase);
            }
            
            // HTML encode the result
            return HttpUtility.HtmlEncode(sanitized);
        }
        
        public bool IsValidFileUpload(IFormFile file, string[] allowedExtensions, long maxSizeBytes)
        {
            if (file == null || file.Length == 0)
                return false;
            
            // Check file size
            if (file.Length > maxSizeBytes)
            {
                _logger.LogWarning("File upload rejected: Size {Size} exceeds limit {Limit}", 
                    file.Length, maxSizeBytes);
                return false;
            }
            
            // Check file extension
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                _logger.LogWarning("File upload rejected: Invalid extension {Extension}", extension);
                return false;
            }
            
            // Check MIME type matches extension
            var expectedMimeTypes = GetExpectedMimeTypes(extension);
            if (!expectedMimeTypes.Contains(file.ContentType.ToLowerInvariant()))
            {
                _logger.LogWarning("File upload rejected: MIME type {MimeType} doesn't match extension {Extension}", 
                    file.ContentType, extension);
                return false;
            }
            
            return true;
        }
        
        public bool IsValidImageFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var maxSize = 5 * 1024 * 1024; // 5MB
            
            return IsValidFileUpload(file, allowedExtensions, maxSize);
        }
        
        public string GenerateSecureFileName(string originalFileName)
        {
            if (string.IsNullOrEmpty(originalFileName))
                return Guid.NewGuid().ToString();
            
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            
            // Remove dangerous characters
            var safeName = Regex.Replace(nameWithoutExtension, @"[^a-zA-Z0-9_-]", "");
            
            // Ensure name is not empty and not too long
            if (string.IsNullOrEmpty(safeName) || safeName.Length > 50)
            {
                safeName = Guid.NewGuid().ToString("N")[..8];
            }
            
            return $"{safeName}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        }
        
        public bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;
            
            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }
        
        public bool ContainsSqlInjectionPatterns(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            
            var lowerInput = input.ToLowerInvariant();
            return _sqlInjectionPatterns.Any(pattern => lowerInput.Contains(pattern));
        }
        
        public bool ContainsXssPatterns(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            
            var lowerInput = input.ToLowerInvariant();
            return _dangerousPatterns.Any(pattern => lowerInput.Contains(pattern.ToLowerInvariant()));
        }
        
        public string EscapeForHtml(string input)
        {
            return HttpUtility.HtmlEncode(input);
        }
        
        public bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return false;
            
            // South African phone number patterns
            var patterns = new[]
            {
                @"^\+27[0-9]{9}$",           // +27123456789
                @"^0[0-9]{9}$",              // 0123456789
                @"^27[0-9]{9}$"              // 27123456789
            };
            
            return patterns.Any(pattern => Regex.IsMatch(phoneNumber, pattern));
        }
        
        public bool IsValidSouthAfricanId(string idNumber)
        {
            if (string.IsNullOrEmpty(idNumber) || idNumber.Length != 13)
                return false;
            
            // Check if all characters are digits
            if (!idNumber.All(char.IsDigit))
                return false;
            
            // Validate using Luhn algorithm for South African ID numbers
            var sum = 0;
            for (var i = 0; i < 12; i++)
            {
                var digit = int.Parse(idNumber[i].ToString());
                if (i % 2 == 1)
                {
                    digit *= 2;
                    if (digit > 9)
                        digit = digit / 10 + digit % 10;
                }
                sum += digit;
            }
            
            var checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit == int.Parse(idNumber[12].ToString());
        }
        
        private static string[] GetExpectedMimeTypes(string extension)
        {
            return extension switch
            {
                ".jpg" or ".jpeg" => new[] { "image/jpeg" },
                ".png" => new[] { "image/png" },
                ".gif" => new[] { "image/gif" },
                ".webp" => new[] { "image/webp" },
                ".pdf" => new[] { "application/pdf" },
                ".doc" => new[] { "application/msword" },
                ".docx" => new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
                ".txt" => new[] { "text/plain" },
                _ => Array.Empty<string>()
            };
        }
    }
}