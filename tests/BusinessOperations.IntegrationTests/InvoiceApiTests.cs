using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessOperations.Application.Common;
using BusinessOperations.Application.Customers;
using BusinessOperations.Application.Invoices;
using BusinessOperations.Domain.Invoices;
using FluentAssertions;

namespace BusinessOperations.IntegrationTests;

public class InvoiceApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public InvoiceApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Fact]
    public async Task Unauthenticated_requests_are_rejected()
    {
        var response = await _client.GetAsync("/api/v1/invoices");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invoice_lifecycle_calculates_balance_and_updates_customer_directory()
    {
        await LoginAsync("admin@local.dev", "ChangeMe!Admin123");
        await EnsureCsrfAsync();

        // 1. Create a customer
        var customerEmail = $"inv-cust-{Guid.NewGuid():N}@example.com";
        var createCustResponse = await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            "Invoice Test Customer",
            customerEmail,
            "555-3333",
            "500 Commerce Way",
            null,
            "Austin",
            "TX",
            "78701",
            "US"));
        createCustResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = await createCustResponse.Content.ReadFromJsonAsync<CustomerDetailsDto>(JsonOptions);
        customer.Should().NotBeNull();

        // 2. Create a draft invoice with 2 items
        var now = DateTime.UtcNow.Date;
        var createInvResponse = await _client.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest(
            customer!.Id,
            now,
            now.AddDays(15),
            new List<CreateInvoiceLineItemRequest>
            {
                new("Setup & Configuration", 2, 250m),
                new("Subscription", 1, 100m)
            }));
        createInvResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var invoice = await createInvResponse.Content.ReadFromJsonAsync<InvoiceDetailsDto>(JsonOptions);
        invoice.Should().NotBeNull();
        invoice!.Status.Should().Be(InvoiceStatus.Draft);
        invoice.TotalAmount.Should().Be(600m);
        invoice.LineItems.Should().HaveCount(2);

        // Before issuing, customer's outstanding balance should still be 0
        var custBeforeIssue = await _client.GetFromJsonAsync<PagedResult<CustomerListItemDto>>(
            $"/api/v1/customers?search={customerEmail}", JsonOptions);
        custBeforeIssue!.Items.Should().ContainSingle();
        custBeforeIssue.Items[0].OutstandingBalance.Should().Be(0m);
        custBeforeIssue.Items[0].OpenInvoiceCount.Should().Be(0);

        // 3. Issue the invoice
        var issueResponse = await _client.PostAsync($"/api/v1/invoices/{invoice.Id}/issue", null);
        issueResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var issuedInvoice = await issueResponse.Content.ReadFromJsonAsync<InvoiceDetailsDto>(JsonOptions);
        issuedInvoice!.Status.Should().Be(InvoiceStatus.Issued);

        // After issuing, customer's outstanding balance must reflect $600 and 1 open invoice
        var custAfterIssue = await _client.GetFromJsonAsync<PagedResult<CustomerListItemDto>>(
            $"/api/v1/customers?search={customerEmail}", JsonOptions);
        custAfterIssue!.Items[0].OutstandingBalance.Should().Be(600m);
        custAfterIssue.Items[0].OpenInvoiceCount.Should().Be(1);

        // 4. Issued invoices cannot be edited
        var editResponse = await _client.PutAsJsonAsync($"/api/v1/invoices/{invoice.Id}", new UpdateInvoiceRequest(
            now,
            now.AddDays(20),
            new List<CreateInvoiceLineItemRequest>
            {
                new("Modified Item", 1, 50m)
            }));
        editResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 5. Void the invoice
        var voidResponse = await _client.PostAsync($"/api/v1/invoices/{invoice.Id}/void", null);
        voidResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var voidedInvoice = await voidResponse.Content.ReadFromJsonAsync<InvoiceDetailsDto>(JsonOptions);
        voidedInvoice!.Status.Should().Be(InvoiceStatus.Void);

        // After voiding, customer's outstanding balance drops back to 0
        var custAfterVoid = await _client.GetFromJsonAsync<PagedResult<CustomerListItemDto>>(
            $"/api/v1/customers?search={customerEmail}", JsonOptions);
        custAfterVoid!.Items[0].OutstandingBalance.Should().Be(0m);
        custAfterVoid.Items[0].OpenInvoiceCount.Should().Be(0);
    }

    private async Task LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task EnsureCsrfAsync()
    {
        var response = await _client.GetAsync("/api/v1/auth/csrf");
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("token").GetString();
        _client.DefaultRequestHeaders.Remove("X-CSRF-TOKEN");
        _client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
    }
}