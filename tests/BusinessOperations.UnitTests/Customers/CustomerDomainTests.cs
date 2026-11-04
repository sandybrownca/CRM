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
            "US");

        customer.Name.Should().Be("Contoso");
        customer.Email.Should().Be("info@contoso.example");
        customer.UpdatedAtUtc.Should().NotBeNull();
        customer.Status.Should().Be(CustomerStatus.Active);
    }

    [Fact]
    public void Deactivate_marks_customer_inactive()
    {
        var customer = new Customer();
        customer.UpdateContact("Acme", "a@example.com", null, null, null, null, null, null, null);

        customer.Deactivate();

        customer.Status.Should().Be(CustomerStatus.Inactive);
        customer.UpdatedAtUtc.Should().NotBeNull();
    }
}
