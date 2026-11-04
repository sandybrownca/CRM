using BusinessOperations.Application.Customers;
using FluentAssertions;

namespace BusinessOperations.UnitTests.Customers;

public class CustomerValidatorTests
{
    private readonly CreateCustomerRequestValidator _validator = new();

    [Fact]
    public async Task Create_requires_name_and_valid_email()
    {
        var result = await _validator.ValidateAsync(new CreateCustomerRequest(
            "",
            "not-an-email",
            null, null, null, null, null, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public async Task Create_accepts_valid_request()
    {
        var result = await _validator.ValidateAsync(new CreateCustomerRequest(
            "Acme Corp",
            "billing@acme.example",
            "555-0100",
            "1 Main St",
            null,
            "Austin",
            "TX",
            "78701",
            "US"));

        result.IsValid.Should().BeTrue();
    }
}
