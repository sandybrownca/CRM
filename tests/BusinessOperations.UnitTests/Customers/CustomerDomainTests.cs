using BusinessOperations.Domain.Customers;
using FluentAssertions;

namespace BusinessOperations.UnitTests.Customers;

public class CustomerDomainTests
{
    [Fact]
    public void UpdateContact_normalizes_email_and_sets_updated_timestamp()
    {
        var customer = new Customer();

        customer.UpdateContact(
            "  Contoso  ",
            "  Info@Contoso.Example ",
            "555-1000",
            "10 Market",
            null,
            "Seattle",
            "WA",
            "98101",
            "US",
            CustomerKind.Company,
            null,
            "20 Dock",
            null,
            "Seattle",
            "WA",
            "98101",
            "US");

        customer.Name.Should().Be("Contoso");
        customer.Email.Should().Be("info@contoso.example");
        customer.Kind.Should().Be(CustomerKind.Company);
        customer.JobTitle.Should().BeNull();
        customer.ShippingAddressLine1.Should().Be("20 Dock");
        customer.UpdatedAtUtc.Should().NotBeNull();
        customer.Status.Should().Be(CustomerStatus.Active);
    }

    [Fact]
    public void UpdateContact_stores_a_job_title_for_an_individual()
    {
        var customer = new Customer();

        customer.UpdateContact(
            "Ada Lovelace",
            "ada@example.com",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CustomerKind.Individual,
            "  Engineer  ",
            null,
            null,
            null,
            null,
            null,
            null);

        customer.Kind.Should().Be(CustomerKind.Individual);
        customer.JobTitle.Should().Be("Engineer");
    }

    [Fact]
    public void UpdateContact_rejects_a_job_title_for_a_company()
    {
        var customer = new Customer();

        var act = () => customer.UpdateContact(
            "Acme",
            "a@example.com",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CustomerKind.Company,
            "Owner",
            null,
            null,
            null,
            null,
            null,
            null);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deactivate_marks_customer_inactive()
    {
        var customer = new Customer();
        customer.UpdateContact(
            "Acme", "a@example.com", null, null, null, null, null, null, null,
            CustomerKind.Company, null, null, null, null, null, null, null);

        customer.Deactivate();

        customer.Status.Should().Be(CustomerStatus.Inactive);
        customer.UpdatedAtUtc.Should().NotBeNull();
    }
}