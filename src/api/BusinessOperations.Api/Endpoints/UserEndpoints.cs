using System.Security.Claims;
using BusinessOperations.Application.Users;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/", async (
            IUserAdminService users,
            CancellationToken cancellationToken,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await users.SearchAsync(search, page, pageSize, cancellationToken);
            return Results.Ok(result);
        });

        group.MapPost("/", async (
            [FromBody] CreateUserRequest request,
            IUserAdminService users,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var created = await users.CreateAsync(request, cancellationToken);
                return Results.Created($"/api/v1/users/{created.Id}", created);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ToErrors(ex));
            }
        });

        group.MapPut("/{id}", async (
            string id,
            [FromBody] UpdateUserRequest request,
            ClaimsPrincipal principal,
            IUserAdminService users,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var actorId = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var updated = await users.UpdateAsync(id, request, actorId, cancellationToken);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ToErrors(ex));
            }
        });

        return group;
    }

    private static Dictionary<string, string[]> ToErrors(ValidationException ex)
    {
        if (ex.Errors.Any())
        {
            return ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => string.IsNullOrWhiteSpace(g.Key) ? "request" : g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());
        }

        return new Dictionary<string, string[]>
        {
            ["request"] = [ex.Message]
        };
    }
}