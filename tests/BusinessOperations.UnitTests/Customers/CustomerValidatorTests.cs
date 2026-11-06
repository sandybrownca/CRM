using BusinessOperations.Application.Customers;
using BusinessOperations.Domain.Customers;
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
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_accepts_an_individual_with_a_job_title()
    {
        var result = await _validator.ValidateAsync(new CreateCustomerRequest(
            "Ada Lovelace",
            "ada@example.com",
            null,
            "1 Main St",
            null,
            "Austin",
            "TX",
            "78701",
            "US",
            CustomerKind.Individual,
            "Engineer"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Create_rejects_a_job_title_on_a_company()
    {
        var result = await _validator.ValidateAsync(new CreateCustomerRequest(
            "Acme Corp",
            "billing@acme.example",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CustomerKind.Company,
            "Owner"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "JobTitle");
    }
}