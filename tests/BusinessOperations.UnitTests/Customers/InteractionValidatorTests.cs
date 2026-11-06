using BusinessOperations.Application.Customers;
using BusinessOperations.Domain.Customers;
using FluentAssertions;

namespace BusinessOperations.UnitTests.Customers;

public class InteractionValidatorTests
{
    private readonly LogInteractionRequestValidator _validator = new();

    [Fact]
    public async Task Log_requires_subject_and_body()
    {
        var result = await _validator.ValidateAsync(new LogInteractionRequest(
            InteractionChannel.Phone,
            " ",
            "",
            null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Subject");
        result.Errors.Should().Contain(e => e.PropertyName == "Body");
    }

    [Fact]
    public async Task Log_accepts_each_channel()
    {
        foreach (var channel in new[]
                 {
                     InteractionChannel.Phone,
                     InteractionChannel.Email,
                     InteractionChannel.Chat,
                     InteractionChannel.Meeting
                 })
        {
            var result = await _validator.ValidateAsync(new LogInteractionRequest(
                channel,
                "Follow-up",
                "Discussed the open invoice.",
                null));

            result.IsValid.Should().BeTrue();
        }
    }
}