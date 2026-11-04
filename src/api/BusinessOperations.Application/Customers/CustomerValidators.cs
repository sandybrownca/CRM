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
    }
}
