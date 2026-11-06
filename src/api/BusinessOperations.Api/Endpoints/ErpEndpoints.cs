using BusinessOperations.Application.Erp;
using BusinessOperations.Domain.Erp;
using BusinessOperations.Domain.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.Api.Endpoints;

public static class ErpEndpoints
{
    public static RouteGroupBuilder MapErpEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1")
            .WithTags("ERP")
            .RequireAuthorization();

        group.MapGet("/erp-syncs", async (
            IErpSyncService syncs,
            CancellationToken cancellationToken,
            [FromQuery] Guid? invoiceId,
            [FromQuery] ErpSyncStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await syncs.SearchAsync(invoiceId, status, page, pageSize, cancellationToken);
            return Results.Ok(result);
        });

        group.MapPost("/invoices/{invoiceId:guid}/erp-sync", async (
            Guid invoiceId,
            IErpSyncService syncs,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await syncs.SyncAsync(invoiceId, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ToErrors(ex));
            }
        })
        .RequireAuthorization(policy => policy.RequireRole(AppRoles.Administrator));

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