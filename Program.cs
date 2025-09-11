using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AccessoryWorld.Data;
using AccessoryWorld.Models;
using AccessoryWorld.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using AccessoryWorld.Validators;
using AccessoryWorld.Security;
using AccessoryWorld.Middleware;
using Microsoft.AspNetCore.Authorization;

namespace AccessoryWorld
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add Entity Framework
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                ??"Server=(localdb)\\MSSQLLocalDB;Database=AccessoryWorldDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";
            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            
            // Add memory cache for performance validation
            builder.Services.AddMemoryCache();
            
            // Add Identity services
            builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 8;
                
                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 5;
                
                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false; // Set to true in production
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
            
            // Add session support for cart
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews(options =>
            {
                // Add global anti-forgery token validation for POST requests
                options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
            });
            
            // Configure anti-forgery options
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-CSRF-TOKEN";
                options.Cookie.Name = "__RequestVerificationToken";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });
            
            // Add FluentValidation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();
            builder.Services.AddValidatorsFromAssemblyContaining<AddToCartValidator>();
            
            // Add authorization policies
            builder.Services.AddAuthorization(options =>
            {
                AuthorizationPolicies.ConfigurePolicies(options);
            });
            
            // Add authorization handlers
            builder.Services.AddScoped<IAuthorizationHandler, OwnerOrAdminHandler>();
            
            // Add custom services
            builder.Services.AddScoped<AccessoryWorld.Services.RoleSeeder>();
            builder.Services.AddScoped<AccessoryWorld.Services.ProductSeeder>();
            builder.Services.AddScoped<AccessoryWorld.Services.ICartService, AccessoryWorld.Services.CartService>();
            builder.Services.AddScoped<AccessoryWorld.Services.IOrderService, AccessoryWorld.Services.OrderService>();
            builder.Services.AddScoped<AccessoryWorld.Services.IPayfastService, AccessoryWorld.Services.PayfastService>();
            builder.Services.AddScoped<AccessoryWorld.Services.IWishlistService, AccessoryWorld.Services.WishlistService>();
            builder.Services.AddScoped<AccessoryWorld.Services.IRecommendationService, AccessoryWorld.Services.RecommendationService>();
            builder.Services.AddScoped<AccessoryWorld.Services.OrderValidationService>();
            builder.Services.AddScoped<AccessoryWorld.Services.ISecurityValidationService, AccessoryWorld.Services.SecurityValidationService>();
            builder.Services.AddScoped<AccessoryWorld.Services.IPaymentValidationService, AccessoryWorld.Services.PaymentValidationService>();
            builder.Services.AddScoped<AccessoryWorld.Services.IWorkflowValidationService, AccessoryWorld.Services.WorkflowValidationService>();
builder.Services.AddScoped<AccessoryWorld.Services.IOrderWorkflowService, AccessoryWorld.Services.OrderWorkflowService>();
            builder.Services.AddScoped<AccessoryWorld.Services.IPerformanceValidationService, AccessoryWorld.Services.PerformanceValidationService>();

            // Configure performance validation options
            builder.Services.Configure<AccessoryWorld.Services.PerformanceValidationOptions>(options =>
            {
                options.MaxRequestsPerMinute = 60;
                options.MaxRequestsPerHour = 1000;
                options.MaxConcurrentRequests = 10;
                options.QueryTimeoutSeconds = 30;
                options.MaxPageSize = 100;
                options.MaxSearchResults = 1000;
                options.MaxUploadSizeBytes = 10 * 1024 * 1024; // 10MB
                options.MaxBulkOperationSize = 100;
                options.EnableRateLimiting = true;
                options.EnableQueryOptimization = true;
            });

            var app = builder.Build();

            // Seed roles, admin user, and sample data
            // Temporarily commented out to test application startup
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var roleSeeder = scope.ServiceProvider.GetRequiredService<AccessoryWorld.Services.RoleSeeder>();
                    await roleSeeder.SeedRolesAsync();
                    await roleSeeder.SeedAdminUserAsync();
                    
                    var productSeeder = scope.ServiceProvider.GetRequiredService<AccessoryWorld.Services.ProductSeeder>();
                    await productSeeder.SeedAsync();
                }
                catch (Exception ex)
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Add performance validation middleware
            // TODO: Fix dependency injection issue with PerformanceValidationMiddleware
            // app.UseMiddleware<PerformanceValidationMiddleware>();

            // Add security middleware
            app.UseSecurityMiddleware(options =>
            {
                options.MaxRequestSize = 10 * 1024 * 1024; // 10MB
                options.RateLimitRequests = 100;
                options.RateLimitWindow = TimeSpan.FromMinutes(1);
            });

            // Temporarily disable HTTPS redirection for testing
            // app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            app.UseSession();
            app.UseRouting();
            
            app.UseAuthentication();
            app.UseCartSessionManagement(); // Handle cart merging after authentication
            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
