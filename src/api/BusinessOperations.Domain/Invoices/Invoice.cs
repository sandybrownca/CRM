using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Customers;

namespace BusinessOperations.Domain.Invoices;

public class Invoice : EntityBase
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime DueDate { get; set; } = DateTime.UtcNow.Date.AddDays(30);
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal Subtotal { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue => Math.Max(0m, TotalAmount - AmountPaid);

    public List<InvoiceLineItem> LineItems { get; set; } = new();

    public void AddLineItem(string description, int quantity, decimal unitPrice)
    {
        EnsureCanEdit();
        LineItems.Add(new InvoiceLineItem(description, quantity, unitPrice)
        {
            InvoiceId = Id
        });
        RecalculateTotals();
    }

    public void ClearLineItems()
    {
        EnsureCanEdit();
        LineItems.Clear();
        RecalculateTotals();
    }

    public void SetSchedule(DateTime issueDate, DateTime dueDate)
    {
        EnsureCanEdit();
        if (dueDate < issueDate)
        {
            throw new InvalidOperationException("DueDate cannot be earlier than IssueDate.");
        }
        IssueDate = issueDate.Date;
        DueDate = dueDate.Date;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecalculateTotals()
    {
        Subtotal = LineItems.Sum(li => li.Quantity * li.UnitPrice);
        TotalAmount = Subtotal;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Cannot issue invoice in '{Status}' status. Only Draft invoices can be issued.");
        }

        if (LineItems.Count == 0)
        {
            throw new InvalidOperationException("Cannot issue an invoice with no line items.");
        }

        RecalculateTotals();

        if (TotalAmount <= 0)
        {
            throw new InvalidOperationException("Cannot issue an invoice with a total amount of zero or less.");
        }

        Status = InvoiceStatus.Issued;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Void()
    {
        if (Status == InvoiceStatus.Void)
        {
            return;
        }

        if (Status == InvoiceStatus.Paid || AmountPaid > 0)
        {
            throw new InvalidOperationException("Cannot void an invoice that has received payments.");
        }

        Status = InvoiceStatus.Void;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RecordPayment(decimal amount)
    {
        if (Status != InvoiceStatus.Issued)
        {
            throw new InvalidOperationException($"Cannot apply payment to invoice in '{Status}' status.");
        }

        if (amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment amount must be greater than zero.");
        }

        if (amount > BalanceDue)
        {
            throw new InvalidOperationException($"Payment of {amount:C} exceeds balance due of {BalanceDue:C}.");
        }

        AmountPaid += amount;
        if (BalanceDue == 0m)
        {
            Status = InvoiceStatus.Paid;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsureCanEdit()
    {
        if (Status != InvoiceStatus.Draft)
        {
            throw new InvalidOperationException($"Invoice '{InvoiceNumber}' cannot be edited because it is in '{Status}' status.");
        }
    }
}
