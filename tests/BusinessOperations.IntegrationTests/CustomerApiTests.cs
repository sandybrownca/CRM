using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessOperations.Application.Common;
using BusinessOperations.Application.Customers;
using BusinessOperations.Domain.Customers;
using FluentAssertions;

namespace BusinessOperations.IntegrationTests;

public class CustomerApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public CustomerApiTests(CustomWebApplicationFactory factory)
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
        var response = await _client.GetAsync("/api/v1/customers");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_can_create_and_list_customers()
    {
        await LoginAsync("admin@local.dev", "ChangeMe!Admin123");
        await EnsureCsrfAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            "Integration Customer",
            $"customer-{Guid.NewGuid():N}@example.com",
            "555-2222",
            "100 Test Ave",
            null,
            "Denver",
            "CO",
            "80202",
            "US"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDetailsDto>(JsonOptions);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Integration Customer");

        var listResponse = await _client.GetAsync("/api/v1/customers?search=Integration");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await listResponse.Content.ReadAsStringAsync();
        payload.Should().Contain(created.Email);
    }

    [Fact]
    public async Task Duplicate_active_email_is_rejected()
    {
        await LoginAsync("ops@local.dev", "ChangeMe!Ops123");
        await EnsureCsrfAsync();

        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var request = new CreateCustomerRequest("First", email, null, null, null, null, null, null, null);

        var first = await _client.PostAsJsonAsync("/api/v1/customers", request);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/v1/customers", request with { Name = "Second" });
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Staff_can_save_an_individual_and_log_an_interaction()
    {
        await LoginAsync("ops@local.dev", "ChangeMe!Ops123");
        await EnsureCsrfAsync();

        var createResponse = await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            "Ada Lovelace",
            $"ada-{Guid.NewGuid():N}@example.com",
            "555-0101",
            "1 Analytical Engine",
            null,
            "London",
            null,
            "SW1A",
            "UK",
            CustomerKind.Individual,
            "Engineer",
            "2 Shipping Lane",
            null,
            "London",
            null,
            "SW1A",
            "UK"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDetailsDto>(JsonOptions);
        created!.Kind.Should().Be(CustomerKind.Individual);
        created.JobTitle.Should().Be("Engineer");
        created.ShippingAddressLine1.Should().Be("2 Shipping Lane");

        var company = await _client.PostAsJsonAsync("/api/v1/customers", new CreateCustomerRequest(
            "Acme Corp",
            $"acme-{Guid.NewGuid():N}@example.com",
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            CustomerKind.Company,
            "Owner"));
        company.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var logResponse = await _client.PostAsJsonAsync(
            $"/api/v1/customers/{created.Id}/interactions",
            new LogInteractionRequest(InteractionChannel.Phone, "Renewal call", "Asked about the open invoice.", null));
        logResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var logged = await logResponse.Content.ReadFromJsonAsync<CustomerInteractionDto>(JsonOptions);
        logged!.Channel.Should().Be(InteractionChannel.Phone);
        logged.RecordedByName.Should().NotBeNullOrWhiteSpace();

        var listResponse = await _client.GetAsync($"/api/v1/customers/{created.Id}/interactions");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var logs = await listResponse.Content.ReadFromJsonAsync<PagedResult<CustomerInteractionDto>>(JsonOptions);
        logs!.Items.Should().Contain(i => i.Id == logged.Id);

        var missing = await _client.GetAsync($"/api/v1/customers/{Guid.NewGuid()}/interactions");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
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