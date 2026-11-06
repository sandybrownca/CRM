using BusinessOperations.Application.Invoices;
using FluentAssertions;

namespace BusinessOperations.UnitTests.Invoices;

public class InvoiceValidatorTests
{
    private readonly CreateInvoiceRequestValidator _createValidator = new();
    private readonly UpdateInvoiceRequestValidator _updateValidator = new();

    [Fact]
    public async Task Create_rejects_missing_customer()
    {
        var request = new CreateInvoiceRequest(
            Guid.Empty,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14),
            new List<CreateInvoiceLineItemRequest>
            {
                new("Item 1", 1, 100m)
            });

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public async Task Create_rejects_due_date_before_issue_date()
    {
        var now = DateTime.UtcNow;
        var request = new CreateInvoiceRequest(
            Guid.NewGuid(),
            now,
            now.AddDays(-1),
            new List<CreateInvoiceLineItemRequest>
            {
                new("Item 1", 1, 100m)
            });

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DueDate");
    }

    [Fact]
    public async Task Create_rejects_empty_line_items()
    {
        var request = new CreateInvoiceRequest(
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14),
            new List<CreateInvoiceLineItemRequest>());

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "LineItems");
    }

    [Fact]
    public async Task Create_rejects_invalid_line_item_quantity_or_price()
    {
        var request = new CreateInvoiceRequest(
            Guid.NewGuid(),
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(14),
            new List<CreateInvoiceLineItemRequest>
            {
                new("", 0, -5m)
            });

        var result = await _createValidator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Description"));
        result.Errors.Should().Contain(e => e.PropertyName.Contains("Quantity"));
        result.Errors.Should().Contain(e => e.PropertyName.Contains("UnitPrice"));
    }

    [Fact]
    public async Task Update_accepts_valid_payload()
    {
        var now = DateTime.UtcNow;
        var request = new UpdateInvoiceRequest(
            now,
            now.AddDays(30),
            new List<CreateInvoiceLineItemRequest>
            {
                new("Consulting", 5, 200m)
            });

        var result = await _updateValidator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }
}