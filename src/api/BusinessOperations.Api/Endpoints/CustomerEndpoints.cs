using System.Security.Claims;
using BusinessOperations.Application.Customers;
using BusinessOperations.Domain.Customers;
using BusinessOperations.Domain.Identity;
using BusinessOperations.Infrastructure.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.Api.Endpoints;

public static class CustomerEndpoints
{
    public static RouteGroupBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/customers")
            .WithTags("Customers")
            .RequireAuthorization();

        group.MapGet("/", async (
            [FromQuery] string? search,
            [FromQuery] int page,
            [FromQuery] int pageSize,
            ICustomerService customers,
            CancellationToken cancellationToken) =>
        {
            var result = await customers.SearchAsync(
                search,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize,
                cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}/activities", async (
            Guid id,
            ICustomerService customers,
            CancellationToken cancellationToken,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await customers.SearchActivitiesAsync(
                id,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize,
                cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapGet("/{id:guid}/interactions", async (
            Guid id,
            ICustomerInteractionService interactions,
            CancellationToken cancellationToken,
            [FromQuery] InteractionChannel? channel = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await interactions.SearchAsync(
                id,
                channel,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize,
                cancellationToken);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/{id:guid}/interactions", async (
            Guid id,
            [FromBody] LogInteractionRequest request,
            ClaimsPrincipal principal,
            UserManager<ApplicationUser> users,
            ICustomerInteractionService interactions,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var user = await users.GetUserAsync(principal);
                var recordedBy = user?.DisplayName;
                if (string.IsNullOrWhiteSpace(recordedBy))
                {
                    recordedBy = principal.Identity?.Name ?? "Staff";
                }

                var created = await interactions.LogAsync(id, request, recordedBy, cancellationToken);
                return created is null
                    ? Results.NotFound()
                    : Results.Created($"/api/v1/customers/{id}/interactions/{created.Id}", created);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ToErrors(ex));
            }
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ICustomerService customers,
            CancellationToken cancellationToken) =>
        {
            var customer = await customers.GetByIdAsync(id, cancellationToken);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        });

        group.MapPost("/", async (
            [FromBody] CreateCustomerRequest request,
            ICustomerService customers,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var created = await customers.CreateAsync(request, cancellationToken);
                return Results.Created($"/api/v1/customers/{created.Id}", created);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ToErrors(ex));
            }
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateCustomerRequest request,
            ICustomerService customers,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var updated = await customers.UpdateAsync(id, request, cancellationToken);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ToErrors(ex));
            }
        });

        group.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            ICustomerService customers,
            CancellationToken cancellationToken) =>
        {
            var deactivated = await customers.DeactivateAsync(id, cancellationToken);
            return deactivated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Operations));

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