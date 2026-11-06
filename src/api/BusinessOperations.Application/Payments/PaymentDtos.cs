
using BusinessOperations.Domain.Invoices;
using BusinessOperations.Domain.Payments;

namespace BusinessOperations.Application.Payments;

public sealed record RecordPaymentRequest(
    decimal Amount,
    DateTime? PaidAtUtc,
    string? Reference,
    PaymentMethod Method = PaymentMethod.Manual,
    string? CardBrand = null,
    string? CardLast4 = null
);

public sealed record PaymentDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    decimal Anmount,
    DateTime PaidAtUtc,
    string? Reference,
    decimal InvoiceAmountPaid,
    decimal InvoiceBalanceDue,
    InvoiceStatus InvoiceStatus,
    DateTime CreatedAtUtc,
    PaymentMethod Method,
    string? CardBrand,
    string? CardLast4,
    string? AuthorizationCode
);

public sealed record PaymentListItemDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    decimal Amount,
    DateTime PaidAtUtc,
    string? Reference,
    DateTime CreatedAtUtc,
    PaymentMethod Method,
    string? CardBrand,
    string? CardLast4,
    string? AuthorizationCode 
);