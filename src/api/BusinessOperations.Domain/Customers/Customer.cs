using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Invoices;

namespace BusinessOperations.Domain.Customers;

public class Customer : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? BillingAddressLine1 { get; set; }
    public string? BillingAddressLine2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; }
    public string? ShippingAddressLine1 { get; set; }
    public string? ShippingAddressLine2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingState { get; set; }
    public string? ShippingPostalCode { get; set; }
    public string? ShippingCountry { get; set; }
    public CustomerKind Kind { get; set; } = CustomerKind.Company;
    public string? JobTitle { get; set; }
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public void Deactivate()
    {
        Status = CustomerStatus.Inactive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateContact(
        string name,
        string email,
        string? phone,
        string? billingAddressLine1,
        string? billingAddressLine2,
        string? billingCity,
        string? billingState,
        string? billingPostalCode,
        string? billingCountry,
        CustomerKind kind,
        string? jobTitle,
        string? shippingAddressLine1,
        string? shippingAddressLine2,
        string? shippingCity,
        string? shippingState,
        string? shippingPostalCode,
        string? shippingCountry)
    {
        if (!Enum.IsDefined(kind))
        {
            throw new InvalidOperationException("Customer kind is not recognized.");
        }

        var title = Clean(jobTitle);
        if (kind == CustomerKind.Company && title is not null)
        {
            throw new InvalidOperationException("Job title applies to an individual customer.");
        }

        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = Clean(phone);
        BillingAddressLine1 = Clean(billingAddressLine1);
        BillingAddressLine2 = Clean(billingAddressLine2);
        BillingCity = Clean(billingCity);
        BillingState = Clean(billingState);
        BillingPostalCode = Clean(billingPostalCode);
        BillingCountry = Clean(billingCountry);
        ShippingAddressLine1 = Clean(shippingAddressLine1);
        ShippingAddressLine2 = Clean(shippingAddressLine2);
        ShippingCity = Clean(shippingCity);
        ShippingState = Clean(shippingState);
        ShippingPostalCode = Clean(shippingPostalCode);
        ShippingCountry = Clean(shippingCountry);
        Kind = kind;
        JobTitle = kind == CustomerKind.Individual ? title : null;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}