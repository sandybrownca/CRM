using BusinessOperations.Application.Invoices;
using BusinessOperations.Domain.Invoices;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static RouteGroupBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/invoices")
            .WithTags("Invoices")
            .RequireAuthorization();

        group.MapGet("/", async (
            IInvoiceService invoices,
            CancellationToken cancellationToken,
            [FromQuery] Guid? customerId,
            [FromQuery] InvoiceStatus? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await invoices.SearchAsync(
                customerId,
                status,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize,
                cancellationToken);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IInvoiceService invoices,
            CancellationToken cancellationToken) =>
        {
            var invoice = await invoices.GetByIdAsync(id, cancellationToken);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        });

        group.MapPost("/", async (
            [FromBody] CreateInvoiceRequest request,
            IInvoiceService invoices,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var created = await invoices.CreateAsync(request, cancellationToken);
                return Results.Created($"/api/v1/invoices/{created.Id}", created);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ToErrors(ex));
            }
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] UpdateInvoiceRequest request,
            IInvoiceService invoices,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var updated = await invoices.UpdateAsync(id, request, cancellationToken);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ToErrors(ex));
            }
        });

        group.MapPost("/{id:guid}/issue", async (
            Guid id,
            IInvoiceService invoices,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var issued = await invoices.IssueAsync(id, cancellationToken);
                return issued is null ? Results.NotFound() : Results.Ok(issued);
            }
            catch (ValidationException ex)
            {
                return Results.ValidationProblem(ToErrors(ex));
            }
        });

        group.MapPost("/{id:guid}/void", async (
            Guid id,
            IInvoiceService invoices,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var voided = await invoices.VoidAsync(id, cancellationToken);
                return voided is null ? Results.NotFound() : Results.Ok(voided);
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