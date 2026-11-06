using System.Text.Json.Serialization;
using BusinessOperations.Api.Endpoints;
using BusinessOperations.Api.Middleware;
using BusinessOperations.Application;
using BusinessOperations.Domain.Identity;
using BusinessOperations.Infrastructure;
using BusinessOperations.Infrastructure.Identity;
using BusinessOperations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BusinessOperations.Infrastructure.Persistence.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(options=>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Administrator));
});

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "bocrm.csrf";
    options.Cookie.HttpOnly = false;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaDev", policy =>
    {
        policy
            .WithOrigins(
                builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
                ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("SpaDev");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AntiforgeryValidationMiddleware>();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapCustomerEndpoints();
app.MapInvoiceEndpoints();
app.MapPaymentEndpoints();
app.MapDashboardEndpoints();
app.MapErpEndpoints();
app.MapUserEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await IdentitySeed.SeedAsync(app.Services);
}
if (app.Environment.IsDevelopment())
{
    await DemoDataSeeder.SeedAsync(app.Services);
}
app.Run();

public partial class Program;
