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
        string? billingCountry)
    {
        Name = name.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = phone?.Trim();
        BillingAddressLine1 = billingAddressLine1?.Trim();
        BillingAddressLine2 = billingAddressLine2?.Trim();
        BillingCity = billingCity?.Trim();
        BillingState = billingState?.Trim();
        BillingPostalCode = billingPostalCode?.Trim();
        BillingCountry = billingCountry?.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
