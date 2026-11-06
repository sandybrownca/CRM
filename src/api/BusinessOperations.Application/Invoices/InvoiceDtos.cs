using BusinessOperations.Domain.Invoices;
using BusinessOperations.Domain.Erp;

namespace BusinessOperations.Application.Invoices;

public sealed record InvoiceLineItemDto(
    Guid Id,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record InvoiceListItemDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    DateTime IssueDate,
    DateTime DueDate,
    InvoiceStatus Status,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    DateTime CreatedAtUtc,
    ErpSyncStatus? ErpStatus,
    string? ErpExternalReference);

public sealed record InvoiceDetailsDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    DateTime IssueDate,
    DateTime DueDate,
    InvoiceStatus Status,
    decimal Subtotal,
    decimal TaxRate,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal BalanceDue,
    IReadOnlyList<InvoiceLineItemDto> LineItems,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateInvoiceLineItemRequest(
    string Description,
    int Quantity,
    decimal UnitPrice);

public sealed record CreateInvoiceRequest(
    Guid CustomerId,
    DateTime IssueDate,
    DateTime DueDate,
    List<CreateInvoiceLineItemRequest> LineItems);

public sealed record UpdateInvoiceRequest(
    DateTime IssueDate,
    DateTime DueDate,
    List<CreateInvoiceLineItemRequest> LineItems);
