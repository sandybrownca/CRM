using BusinessOperations.Application.Customers;
using BusinessOperations.Application.Invoices;
using BusinessOperations.Application.Erp;
using BusinessOperations.Application.Dashboard;
using BusinessOperations.Application.Payments;
using BusinessOperations.Application.Users;
using BusinessOperations.Infrastructure.Customers;
using BusinessOperations.Infrastructure.Dashboard;
using BusinessOperations.Infrastructure.Erp;
using BusinessOperations.Infrastructure.Identity;
using BusinessOperations.Infrastructure.Invoices;
using BusinessOperations.Infrastructure.Payments;
using BusinessOperations.Infrastructure.Users;
using BusinessOperations.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace BusinessOperations.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "bocrm.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnValidatePrincipal = async context =>
            {
                await SecurityStampValidator.ValidatePrincipalAsync(context);
                if(context.Principal is null)
                {
                    return;
                }
                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.GetUserAsync(context.Principal);
                if(user is null || !user.IsActive)
                {
                    context.RejectPrincipal();
                }
            };
        });

        services.AddSingleton<SimulatedErpClient>();
        services.AddSingleton<IErpClient>(sp=> sp.GetRequiredService<SimulatedErpClient>());
        services.AddScoped<IErpSyncService, ErpSyncService>();        
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerInteractionService, CustomerInteractionService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<IPaymentService, PaymentService>();      
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IUserAdminService, UserAdminService>();

        return services;
    }
}
