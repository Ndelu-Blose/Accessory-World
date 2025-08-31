using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AccessoryWorld.Security
{
    public static class AuthorizationPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string StaffOnly = "StaffOnly";
        public const string CustomerOnly = "CustomerOnly";
        public const string AdminOrStaff = "AdminOrStaff";
        public const string OwnerOrAdmin = "OwnerOrAdmin";
        
        public static void ConfigurePolicies(AuthorizationOptions options)
        {
            // Admin only policy
            options.AddPolicy(AdminOnly, policy =>
                policy.RequireRole("Admin"));
            
            // Staff only policy (includes Admin)
            options.AddPolicy(StaffOnly, policy =>
                policy.RequireRole("Admin", "Staff"));
            
            // Customer only policy
            options.AddPolicy(CustomerOnly, policy =>
                policy.RequireRole("Customer"));
            
            // Admin or Staff policy
            options.AddPolicy(AdminOrStaff, policy =>
                policy.RequireRole("Admin", "Staff"));
            
            // Owner or Admin policy (for accessing own resources)
            options.AddPolicy(OwnerOrAdmin, policy =>
                policy.Requirements.Add(new OwnerOrAdminRequirement()));
        }
    }
    
    public class OwnerOrAdminRequirement : IAuthorizationRequirement
    {
    }
    
    public class OwnerOrAdminHandler : AuthorizationHandler<OwnerOrAdminRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            OwnerOrAdminRequirement requirement)
        {
            var user = context.User;
            
            // Allow if user is Admin
            if (user.IsInRole("Admin"))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            
            // Check if user owns the resource (this would need to be customized per resource)
            // For now, we'll just check if they're authenticated
            if (user.Identity?.IsAuthenticated == true)
            {
                context.Succeed(requirement);
            }
            
            return Task.CompletedTask;
        }
    }
}