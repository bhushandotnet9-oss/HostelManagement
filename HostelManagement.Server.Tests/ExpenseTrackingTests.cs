using System.Net;
using System.Net.Http.Json;
using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Models;

namespace HostelManagement.Server.Tests;

public class ExpenseTrackingTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<HttpClient> AuthenticatedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var setupResponse = await client.PostAsJsonAsync("/api/account/setup",
            new SetupRequest("Pune Hostels", email, "P@ssw0rd!"));
        var auth = await setupResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new("Bearer", auth!.Token);
        return client;
    }

    private static async Task<Guid> CreateHostelAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/hostels", new CreateHostelRequest("Hostel One", "Pune"));
        var hostel = await response.Content.ReadFromJsonAsync<HostelResponse>();
        return hostel!.Id;
    }

    [Fact]
    public async Task Expense_cannot_be_saved_without_a_valid_hostel()
    {
        var client = await AuthenticatedClientAsync("expense1@example.com");

        var response = await client.PostAsJsonAsync($"/api/hostels/{Guid.NewGuid()}/expenses",
            new CreateExpenseRequest(ExpenseCategory.Electricity, 500m, new DateOnly(2026, 1, 10), null), TestJson.Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Expense_cannot_be_saved_with_an_invalid_category()
    {
        var client = await AuthenticatedClientAsync("expense2@example.com");
        var hostelId = await CreateHostelAsync(client);

        var response = await client.PostAsync($"/api/hostels/{hostelId}/expenses",
            JsonContent.Create(new { category = "NotARealCategory", amount = 500m, date = "2026-01-10" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Expense_can_be_saved_under_the_other_category()
    {
        var client = await AuthenticatedClientAsync("expense3@example.com");
        var hostelId = await CreateHostelAsync(client);

        var response = await client.PostAsJsonAsync($"/api/hostels/{hostelId}/expenses",
            new CreateExpenseRequest(ExpenseCategory.Other, 500m, new DateOnly(2026, 1, 10), "Miscellaneous"), TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Breakdown_sums_totals_correctly_across_multiple_entries_and_categories()
    {
        var client = await AuthenticatedClientAsync("expense4@example.com");
        var hostelId = await CreateHostelAsync(client);

        await client.PostAsJsonAsync($"/api/hostels/{hostelId}/expenses",
            new CreateExpenseRequest(ExpenseCategory.Electricity, 1000m, new DateOnly(2026, 1, 5), null), TestJson.Options);
        await client.PostAsJsonAsync($"/api/hostels/{hostelId}/expenses",
            new CreateExpenseRequest(ExpenseCategory.Electricity, 500m, new DateOnly(2026, 1, 20), null), TestJson.Options);
        await client.PostAsJsonAsync($"/api/hostels/{hostelId}/expenses",
            new CreateExpenseRequest(ExpenseCategory.Wifi, 300m, new DateOnly(2026, 1, 15), null), TestJson.Options);
        // Outside the requested date range - should not be counted.
        await client.PostAsJsonAsync($"/api/hostels/{hostelId}/expenses",
            new CreateExpenseRequest(ExpenseCategory.Electricity, 9999m, new DateOnly(2026, 3, 1), null), TestJson.Options);

        var response = await client.GetAsync($"/api/hostels/{hostelId}/expenses/breakdown?from=2026-01-01&to=2026-01-31");
        var totals = await response.Content.ReadFromJsonAsync<List<ExpenseCategoryTotal>>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1500m, totals!.Single(t => t.Category == ExpenseCategory.Electricity).Total);
        Assert.Equal(300m, totals.Single(t => t.Category == ExpenseCategory.Wifi).Total);
    }
}
