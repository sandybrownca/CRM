using System.Security.Claims;
using BusinessOperations.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        group.MapPost("/login", async (
            [FromBody] LoginRequest request,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = ["Email and password are required."]
                });
            }

            var user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null || !user.IsActive)
            {
                return Results.Unauthorized();
            }

            var result = await signInManager.PasswordSignInAsync(
                user,
                request.Password,
                isPersistent: true,
                lockoutOnFailure: true);

            if (!result.Succeeded)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            return Results.Ok(new AuthUserResponse(user.Id, user.Email!, user.DisplayName, roles.ToArray()));
        })
        .AllowAnonymous();

        group.MapPost("/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        })
        .RequireAuthorization();

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> userManager) =>
        {
            var user = await userManager.GetUserAsync(principal);
            if (user is null || !user.IsActive)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            return Results.Ok(new AuthUserResponse(user.Id, user.Email!, user.DisplayName, roles.ToArray()));
        })
        .RequireAuthorization();

        group.MapGet("/csrf", (IAntiforgery antiforgery, HttpContext httpContext) =>
        {
            var tokens = antiforgery.GetAndStoreTokens(httpContext);
            return Results.Ok(new { token = tokens.RequestToken });
        })
        .AllowAnonymous();

        return group;
    }
}

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthUserResponse(string Id, string Email, string DisplayName, string[] Roles);
