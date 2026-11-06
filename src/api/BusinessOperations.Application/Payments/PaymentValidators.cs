using BusinessOperations.Domain.Payments;
using FluentValidation;

namespace BusinessOperations.Application.Payments;

public sealed class RecordPaymentRequestValidator : AbstractValidator<RecordPaymentRequest>
{
    public RecordPaymentRequestValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Payment amount must be greater than zero.")
            .Must(HasAtMostTwoDecimalPlaces)
            .WithMessage("Payment amount cannot have more than two decimal places.");

        RuleFor(x => x.Reference)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Reference));

        RuleFor(x => x.Reference)
            .Must(value => !ContainsCardNumber(value))
            .WithMessage("Do not enter a card number. A simulated card payment stores only the last four digits.");

        RuleFor(x => x.CardBrand)
            .NotEmpty()
            .MaximumLength(20)
            .Must(value => !ContainsCardNumber(value))
            .WithMessage("Enter the card brand, not the card number.")
            .When(x => x.Method == PaymentMethod.SimulatedCard);

        RuleFor(x => x.CardLast4)
            .Matches("^\\d{4}$")
            .WithMessage("Enter only the last four digits.")
            .When(x => x.Method == PaymentMethod.SimulatedCard);

        RuleFor(x => x.CardBrand)
            .Must(string.IsNullOrWhiteSpace)
            .When(x => x.Method != PaymentMethod.SimulatedCard);

        RuleFor(x => x.CardLast4)
            .Must(string.IsNullOrWhiteSpace)
            .When(x => x.Method != PaymentMethod.SimulatedCard);
    }

    private static bool ContainsCardNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length is >= 13 and <= 19;
    }

    private static bool HasAtMostTwoDecimalPlaces(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero) == amount;
}