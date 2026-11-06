using BusinessOperations.Domain.Customers;

namespace BusinessOperations.Application.Customers;

public sealed record CustomerListItemDto(
    Guid Id,
    string Name,
    string Email,
    CustomerStatus Status,
    decimal OutstandingBalance,
    int OpenInvoiceCount,
    CustomerKind Kind,
    string? JobTitle,
    string? BillingCity);

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
    DateTime? UpdatedAtUtc,
    CustomerKind Kind,
    string? JobTitle,
    string? ShippingAddressLine1,
    string? ShippingAddressLine2,
    string? ShippingCity,
    string? ShippingState,
    string? ShippingPostalCode,
    string? ShippingCountry);

public sealed record CustomerActivityDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string EventType,
    string Description,
    Guid? RelatedRecordId,
    DateTime OccurredAtUtc);

public sealed record CreateCustomerRequest(
    string Name,
    string Email,
    string? Phone,
    string? BillingAddressLine1,
    string? BillingAddressLine2,
    string? BillingCity,
    string? BillingState,
    string? BillingPostalCode,
    string? BillingCountry,
    CustomerKind Kind = CustomerKind.Company,
    string? JobTitle = null,
    string? ShippingAddressLine1 = null,
    string? ShippingAddressLine2 = null,
    string? ShippingCity = null,
    string? ShippingState = null,
    string? ShippingPostalCode = null,
    string? ShippingCountry = null);

public sealed record UpdateCustomerRequest(
    string Name,
    string Email,
    string? Phone,
    string? BillingAddressLine1,
    string? BillingAddressLine2,
    string? BillingCity,
    string? BillingState,
    string? BillingPostalCode,
    string? BillingCountry,
    CustomerKind Kind = CustomerKind.Company,
    string? JobTitle = null,
    string? ShippingAddressLine1 = null,
    string? ShippingAddressLine2 = null,
    string? ShippingCity = null,
    string? ShippingState = null,
    string? ShippingPostalCode = null,
    string? ShippingCountry = null);

public sealed record LogInteractionRequest(
    InteractionChannel Channel,
    string Subject,
    string Body,
    DateTime? OccurredAtUtc);

public sealed record CustomerInteractionDto(
    Guid Id,
    Guid CustomerId,
    InteractionChannel Channel,
    string Subject,
    string Body,
    DateTime OccurredAtUtc,
    string RecordedByName);