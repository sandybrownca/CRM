using BusinessOperations.Domain.Customers;
using FluentValidation;

namespace BusinessOperations.Application.Customers;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone is not null);
        RuleFor(x => x.BillingAddressLine1).MaximumLength(200).When(x => x.BillingAddressLine1 is not null);
        RuleFor(x => x.BillingAddressLine2).MaximumLength(200).When(x => x.BillingAddressLine2 is not null);
        RuleFor(x => x.BillingCity).MaximumLength(100).When(x => x.BillingCity is not null);
        RuleFor(x => x.BillingState).MaximumLength(100).When(x => x.BillingState is not null);
        RuleFor(x => x.BillingPostalCode).MaximumLength(20).When(x => x.BillingPostalCode is not null);
        RuleFor(x => x.BillingCountry).MaximumLength(100).When(x => x.BillingCountry is not null);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.JobTitle)
            .MaximumLength(100)
            .When(x => x.Kind == CustomerKind.Individual && x.JobTitle is not null);
        RuleFor(x => x.JobTitle)
            .Must(title => string.IsNullOrWhiteSpace(title))
            .When(x => x.Kind == CustomerKind.Company)
            .WithMessage("Job title applies to an individual customer.");
        RuleFor(x => x.ShippingAddressLine1).MaximumLength(200).When(x => x.ShippingAddressLine1 is not null);
        RuleFor(x => x.ShippingAddressLine2).MaximumLength(200).When(x => x.ShippingAddressLine2 is not null);
        RuleFor(x => x.ShippingCity).MaximumLength(100).When(x => x.ShippingCity is not null);
        RuleFor(x => x.ShippingState).MaximumLength(100).When(x => x.ShippingState is not null);
        RuleFor(x => x.ShippingPostalCode).MaximumLength(20).When(x => x.ShippingPostalCode is not null);
        RuleFor(x => x.ShippingCountry).MaximumLength(100).When(x => x.ShippingCountry is not null);
    }
}

public sealed class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone is not null);
        RuleFor(x => x.BillingAddressLine1).MaximumLength(200).When(x => x.BillingAddressLine1 is not null);
        RuleFor(x => x.BillingAddressLine2).MaximumLength(200).When(x => x.BillingAddressLine2 is not null);
        RuleFor(x => x.BillingCity).MaximumLength(100).When(x => x.BillingCity is not null);
        RuleFor(x => x.BillingState).MaximumLength(100).When(x => x.BillingState is not null);
        RuleFor(x => x.BillingPostalCode).MaximumLength(20).When(x => x.BillingPostalCode is not null);
        RuleFor(x => x.BillingCountry).MaximumLength(100).When(x => x.BillingCountry is not null);
        RuleFor(x => x.Kind).IsInEnum();
        RuleFor(x => x.JobTitle)
            .MaximumLength(100)
            .When(x => x.Kind == CustomerKind.Individual && x.JobTitle is not null);
        RuleFor(x => x.JobTitle)
            .Must(title => string.IsNullOrWhiteSpace(title))
            .When(x => x.Kind == CustomerKind.Company)
            .WithMessage("Job title applies to an individual customer.");
        RuleFor(x => x.ShippingAddressLine1).MaximumLength(200).When(x => x.ShippingAddressLine1 is not null);
        RuleFor(x => x.ShippingAddressLine2).MaximumLength(200).When(x => x.ShippingAddressLine2 is not null);
        RuleFor(x => x.ShippingCity).MaximumLength(100).When(x => x.ShippingCity is not null);
        RuleFor(x => x.ShippingState).MaximumLength(100).When(x => x.ShippingState is not null);
        RuleFor(x => x.ShippingPostalCode).MaximumLength(20).When(x => x.ShippingPostalCode is not null);
        RuleFor(x => x.ShippingCountry).MaximumLength(100).When(x => x.ShippingCountry is not null);
    }
}

public sealed class LogInteractionRequestValidator : AbstractValidator<LogInteractionRequest>
{
    public LogInteractionRequestValidator()
    {
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(4000);
    }
}