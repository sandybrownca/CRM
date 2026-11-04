using BusinessOperations.Domain.Invoices;
using FluentAssertions;

namespace BusinessOperations.UnitTests.Invoices;

public class InvoiceDomainTests
{
    [Fact]
    public void AddLineItem_recalculates_subtotal_and_total_using_decimal_precision()
    {
        var invoice = new Invoice();
        invoice.AddLineItem("Consulting Hours", 10, 150.25m);
        invoice.AddLineItem("Platform Fee", 1, 99.99m);

        invoice.Subtotal.Should().Be(1602.49m);
        invoice.TotalAmount.Should().Be(1602.49m);
        invoice.BalanceDue.Should().Be(1602.49m);
        invoice.Status.Should().Be(InvoiceStatus.Draft);
    }

    [Fact]
    public void Issue_transitions_status_to_issued()
    {
        var invoice = new Invoice();
        invoice.AddLineItem("Implementation", 1, 500m);

        invoice.Issue();

        invoice.Status.Should().Be(InvoiceStatus.Issued);
        invoice.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Issue_throws_if_no_line_items()
    {
        var invoice = new Invoice();

        var action = () => invoice.Issue();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*no line items*");
    }

    [Fact]
    public void Issue_throws_if_total_is_zero()
    {
        var invoice = new Invoice();
        invoice.AddLineItem("Free Trial", 1, 0m);

        var action = () => invoice.Issue();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*zero or less*");
    }

    [Fact]
    public void Cannot_edit_issued_invoice()
    {
        var invoice = new Invoice();
        invoice.AddLineItem("Service A", 1, 200m);
        invoice.Issue();

        var addAction = () => invoice.AddLineItem("Service B", 1, 100m);
        var clearAction = () => invoice.ClearLineItems();
        var scheduleAction = () => invoice.SetSchedule(DateTime.UtcNow, DateTime.UtcNow.AddDays(10));

        addAction.Should().Throw<InvalidOperationException>();
        clearAction.Should().Throw<InvalidOperationException>();
        scheduleAction.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Void_transitions_status_to_void()
    {
        var invoice = new Invoice();
        invoice.AddLineItem("Design", 1, 300m);
        invoice.Issue();

        invoice.Void();

        invoice.Status.Should().Be(InvoiceStatus.Void);
    }

    [Fact]
    public void RecordPayment_reduces_balance_and_marks_paid_when_settled()
    {
        var invoice = new Invoice();
        invoice.AddLineItem("Retainer", 1, 1000m);
        invoice.Issue();

        invoice.RecordPayment(400m);
        invoice.AmountPaid.Should().Be(400m);
        invoice.BalanceDue.Should().Be(600m);
        invoice.Status.Should().Be(InvoiceStatus.Issued);

        invoice.RecordPayment(600m);
        invoice.AmountPaid.Should().Be(1000m);
        invoice.BalanceDue.Should().Be(0m);
        invoice.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void Cannot_void_invoice_with_received_payments()
    {
        var invoice = new Invoice();
        invoice.AddLineItem("Hosting", 1, 100m);
        invoice.Issue();
        invoice.RecordPayment(50m);

        var action = () => invoice.Void();

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*received payments*");
    }
}
