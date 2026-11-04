using BusinessOperations.Domain.Common;

namespace BusinessOperations.Domain.Invoices;

public class InvoiceLineItem : EntityBase
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;

    public InvoiceLineItem() { }

    public InvoiceLineItem(string description, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "UnitPrice cannot be negative.");

        Description = description.Trim();
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
