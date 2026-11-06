using BusinessOperations.Application.Payments;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.Api.Endpoints;

public static class PaymentEndpoints
{
    public static RouteGroupBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1")
            .WithTags("Payments")
            .RequireAuthorization();

        group.MapGet("/payments", async (
            IPaymentService payments,
            CancellationToken cancellationToken,
            [FromQuery] Guid? invoiceId,
            [FromQuery] Guid? customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await payments.SearchAsync(
                invoiceId,
                customerId,
                page <= 0 ? 1 : page,
                pageSize <= 0 ? 20 : pageSize,
                cancellationToken);
            return Results.Ok(result);
        });

        group.MapPost("/invoices/{invoiceId:guid}/payments", async (
            Guid invoiceId,
            [FromBody] RecordPaymentRequest request,
            IPaymentService payments,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var recorded = await payments.RecordAsync(invoiceId, request, cancellationToken);
                return recorded is null
                    ? Results.NotFound()
                    : Results.Created($"/api/v1/payments?invoiceId={recorded.InvoiceId}", recorded);
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