using BusinessOperations.Domain.Customers;

namespace BusinessOperations.Application.Customers;

public sealed record CustomerListItemDto(
    Guid Id,
    string Name,
    string Email,
    CustomerStatus Status,
    decimal OutstandingBalance,
    int OpenInvoiceCount);

public sealed record CustomerDetailsDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    string? BillingAddressLine1,
    string? BillingAddressLine2,
    string? BillingCity,
    string? BillingState,
    string? BillingPostalCode,
    string? BillingCountry,
    CustomerStatus Status,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record CreateCustomerRequest(
    string Name,
    string Email,
    string? Phone,
    string? BillingAddressLine1,
    string? BillingAddressLine2,
    string? BillingCity,
    string? BillingState,
    string? BillingPostalCode,
    string? BillingCountry);

public sealed record UpdateCustomerRequest(
    string Name,
    string Email,
    string? Phone,
    string? BillingAddressLine1,
    string? BillingAddressLine2,
    string? BillingCity,
    string? BillingState,
    string? BillingPostalCode,
    string? BillingCountry);
