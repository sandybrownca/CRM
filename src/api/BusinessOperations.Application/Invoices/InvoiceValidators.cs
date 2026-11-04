using FluentValidation;

namespace BusinessOperations.Application.Invoices;

public sealed class CreateInvoiceLineItemRequestValidator : AbstractValidator<CreateInvoiceLineItemRequest>
{
    public CreateInvoiceLineItemRequestValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Line item description is required.")
            .MaximumLength(200);

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0m).WithMessage("Unit price cannot be negative.");
    }
}

public sealed class CreateInvoiceRequestValidator : AbstractValidator<CreateInvoiceRequest>
{
    public CreateInvoiceRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer is required.");

        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("Due date cannot be earlier than issue date.");

        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("Invoice must have at least one line item.");

        RuleForEach(x => x.LineItems)
            .SetValidator(new CreateInvoiceLineItemRequestValidator());
    }
}

public sealed class UpdateInvoiceRequestValidator : AbstractValidator<UpdateInvoiceRequest>
{
    public UpdateInvoiceRequestValidator()
    {
        RuleFor(x => x.DueDate)
            .GreaterThanOrEqualTo(x => x.IssueDate)
            .WithMessage("Due date cannot be earlier than issue date.");

        RuleFor(x => x.LineItems)
            .NotEmpty().WithMessage("Invoice must have at least one line item.");

        RuleForEach(x => x.LineItems)
            .SetValidator(new CreateInvoiceLineItemRequestValidator());
    }
}
