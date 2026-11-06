using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessOperations.Application.Common;
using BusinessOperations.Application.Customers;
using BusinessOperations.Application.Dashboard;
using BusinessOperations.Application.Invoices;
using BusinessOperations.Domain.Invoices;
using FluentAssertions;

namespace BusinessOperations.IntegrationTests;

public class DashboardApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public DashboardApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }

    [Fact]
    public async Task Unauthenticated_dashboard_requests_are_rejected()
    {
        var response = await _client.GetAsync("/api/v1/dashboard");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Dashboard_counts_issued_balances_and_customer_history()
    {
        await LoginAsync();
        await EnsureCsrfAsync();

        var email = $"dash-{Guid.NewGuid():N}@example.com";
        var createCustomer = await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            "Dashboard Customer",
            email,
            null,
            null,
            null,
            null,
            null,
            null,
            null));
        createCustomer.StatusCode.Should().Be(HttpStatusCode.Created);
        var customer = (await createCustomer.Content.ReadFromJsonAsync<CustomerDetailsDto>(JsonOptions))!;

        var issueDate = DateTime.UtcNow.Date.AddDays(-10);
        var createInvoice = await _client.PostAsJsonAsync("/api/v1/invoices", new CreateInvoiceRequest(
            customer.Id,
            issueDate,
            issueDate.AddDays(5),
            new List<CreateInvoiceLineItemRequest> { new("Overdue service", 1, 80m) }));
        createInvoice.StatusCode.Should().Be(HttpStatusCode.Created);
        var draft = (await createInvoice.Content.ReadFromJsonAsync<InvoiceDetailsDto>(JsonOptions))!;

        var beforeIssue = await _client.GetFromJsonAsync<DashboardDto>("/api/v1/dashboard", JsonOptions);
        beforeIssue!.OpenInvoices.Should().NotContain(i => i.Id == draft.Id);

        var issue = await _client.PostAsync($"/api/v1/invoices/{draft.Id}/issue", null);
        issue.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterIssue = await _client.GetFromJsonAsync<DashboardDto>("/api/v1/dashboard", JsonOptions);
        afterIssue!.OpenInvoiceCount.Should().BeGreaterThan(beforeIssue.OpenInvoiceCount);
        afterIssue.OutstandingBalance.Should().Be(beforeIssue.OutstandingBalance + 80m);
        afterIssue.OverdueInvoiceCount.Should().BeGreaterThan(0);
        afterIssue.OpenInvoices.Should().Contain(i => i.Id == draft.Id && i.BalanceDue == 80m && i.Status == InvoiceStatus.Issued);
        afterIssue.ActiveCustomerCount.Should().BeGreaterThan(0);

        var pay = await _client.PostAsJsonAsync(
            $"/api/v1/invoices/{draft.Id}/payments",
            new { amount = 30m, reference = "ACH" });
        pay.StatusCode.Should().Be(HttpStatusCode.Created);

        var afterPay = await _client.GetFromJsonAsync<DashboardDto>("/api/v1/dashboard", JsonOptions);
        afterPay!.OutstandingBalance.Should().Be(beforeIssue.OutstandingBalance + 50m);
        afterPay.RecentActivity.Should().Contain(a => a.CustomerId == customer.Id && a.EventType == "PaymentRecorded");

        var history = await _client.GetFromJsonAsync<PagedResult<CustomerActivityDto>>(
            $"/api/v1/customers/{customer.Id}/activities?page=1&pageSize=20",
            JsonOptions);
        history!.Items.Select(a => a.EventType).Should().Contain(new[] { "CustomerCreated", "InvoiceCreated", "InvoiceIssued", "PaymentRecorded" });

        var missing = await _client.GetAsync($"/api/v1/customers/{Guid.NewGuid()}/activities");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task LoginAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email = "admin@local.dev", password = "ChangeMe!Admin123" });
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