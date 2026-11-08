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
    public async Task Expense_entries_are_listable_for_a_hostel()
    {
        var client = await AuthenticatedClientAsync("expense5@example.com");
        var hostelId = await CreateHostelAsync(client);
        await CreateExpenseAsync(client, hostelId, ExpenseCategory.Electricity, 1000m);
        await CreateExpenseAsync(client, hostelId, ExpenseCategory.Wifi, 300m);

        var response = await client.GetAsync($"/api/hostels/{hostelId}/expenses");
        var expenses = await response.Content.ReadFromJsonAsync<List<ExpenseResponse>>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, expenses!.Count);
        Assert.All(expenses, e => Assert.NotEqual(Guid.Empty, e.Id));
    }

    [Fact]
    public async Task A_single_expense_entry_can_be_read()
    {
        var client = await AuthenticatedClientAsync("expense6@example.com");
        var hostelId = await CreateHostelAsync(client);
        var expense = await CreateExpenseAsync(client, hostelId, ExpenseCategory.Staff, 7000m);

        var response = await client.GetAsync($"/api/hostels/{hostelId}/expenses/{expense.Id}");
        var fetched = await response.Content.ReadFromJsonAsync<ExpenseResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ExpenseCategory.Staff, fetched!.Category);
        Assert.Equal(7000m, fetched.Amount);
    }

    [Fact]
    public async Task Breakdown_route_still_resolves_alongside_the_single_entry_route()
    {
        var client = await AuthenticatedClientAsync("expense7@example.com");
        var hostelId = await CreateHostelAsync(client);
        await CreateExpenseAsync(client, hostelId, ExpenseCategory.Wifi, 300m);

        var response = await client.GetAsync($"/api/hostels/{hostelId}/expenses/breakdown?from=2026-01-01&to=2026-12-31");
        var totals = await response.Content.ReadFromJsonAsync<List<ExpenseCategoryTotal>>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(300m, totals!.Single(t => t.Category == ExpenseCategory.Wifi).Total);
    }

    [Fact]
    public async Task Expense_amount_can_be_corrected_and_the_breakdown_follows()
    {
        var client = await AuthenticatedClientAsync("expense8@example.com");
        var hostelId = await CreateHostelAsync(client);
        var expense = await CreateExpenseAsync(client, hostelId, ExpenseCategory.Electricity, 1000m);

        var response = await client.PutAsJsonAsync($"/api/hostels/{hostelId}/expenses/{expense.Id}",
            new UpdateExpenseRequest(ExpenseCategory.Electricity, 1250m, new DateOnly(2026, 1, 10), "Corrected"),
            TestJson.Options);
        var updated = await response.Content.ReadFromJsonAsync<ExpenseResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1250m, updated!.Amount);

        var totals = await client.GetFromJsonAsync<List<ExpenseCategoryTotal>>(
            $"/api/hostels/{hostelId}/expenses/breakdown?from=2026-01-01&to=2026-01-31", TestJson.Options);
        Assert.Equal(1250m, totals!.Single(t => t.Category == ExpenseCategory.Electricity).Total);
    }

    [Fact]
    public async Task Recategorizing_an_expense_moves_its_amount_between_categories()
    {
        var client = await AuthenticatedClientAsync("expense9@example.com");
        var hostelId = await CreateHostelAsync(client);
        var expense = await CreateExpenseAsync(client, hostelId, ExpenseCategory.Electricity, 1000m);

        await client.PutAsJsonAsync($"/api/hostels/{hostelId}/expenses/{expense.Id}",
            new UpdateExpenseRequest(ExpenseCategory.Maintenance, 1000m, new DateOnly(2026, 1, 10), null),
            TestJson.Options);

        var totals = await client.GetFromJsonAsync<List<ExpenseCategoryTotal>>(
            $"/api/hostels/{hostelId}/expenses/breakdown?from=2026-01-01&to=2026-01-31", TestJson.Options);
        Assert.DoesNotContain(totals!, t => t.Category == ExpenseCategory.Electricity);
        Assert.Equal(1000m, totals!.Single(t => t.Category == ExpenseCategory.Maintenance).Total);
    }

    [Fact]
    public async Task Expense_update_with_a_non_positive_amount_is_rejected()
    {
        var client = await AuthenticatedClientAsync("expense10@example.com");
        var hostelId = await CreateHostelAsync(client);
        var expense = await CreateExpenseAsync(client, hostelId, ExpenseCategory.Electricity, 1000m);

        var response = await client.PutAsJsonAsync($"/api/hostels/{hostelId}/expenses/{expense.Id}",
            new UpdateExpenseRequest(ExpenseCategory.Electricity, 0m, new DateOnly(2026, 1, 10), null),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var unchanged = await client.GetFromJsonAsync<ExpenseResponse>($"/api/hostels/{hostelId}/expenses/{expense.Id}", TestJson.Options);
        Assert.Equal(1000m, unchanged!.Amount);
    }

    [Fact]
    public async Task Expense_can_be_deleted_and_leaves_the_list_and_breakdown()
    {
        var client = await AuthenticatedClientAsync("expense11@example.com");
        var hostelId = await CreateHostelAsync(client);
        var expense = await CreateExpenseAsync(client, hostelId, ExpenseCategory.Electricity, 1000m);

        var response = await client.DeleteAsync($"/api/hostels/{hostelId}/expenses/{expense.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var expenses = await client.GetFromJsonAsync<List<ExpenseResponse>>($"/api/hostels/{hostelId}/expenses", TestJson.Options);
        Assert.DoesNotContain(expenses!, e => e.Id == expense.Id);

        var totals = await client.GetFromJsonAsync<List<ExpenseCategoryTotal>>(
            $"/api/hostels/{hostelId}/expenses/breakdown?from=2026-01-01&to=2026-01-31", TestJson.Options);
        Assert.DoesNotContain(totals!, t => t.Category == ExpenseCategory.Electricity);
    }

    [Fact]
    public async Task Another_orgs_expense_cannot_be_updated_or_deleted()
    {
        var ownerA = await AuthenticatedClientAsync("expense12a@example.com");
        var ownerB = await AuthenticatedClientAsync("expense12b@example.com");
        var hostelOfA = await CreateHostelAsync(ownerA);
        var expenseOfA = await CreateExpenseAsync(ownerA, hostelOfA, ExpenseCategory.Electricity, 1000m);

        var updateResponse = await ownerB.PutAsJsonAsync($"/api/hostels/{hostelOfA}/expenses/{expenseOfA.Id}",
            new UpdateExpenseRequest(ExpenseCategory.Other, 1m, new DateOnly(2026, 1, 10), "Hijacked"),
            TestJson.Options);
        var deleteResponse = await ownerB.DeleteAsync($"/api/hostels/{hostelOfA}/expenses/{expenseOfA.Id}");

        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);

        var stillOriginal = await ownerA.GetFromJsonAsync<ExpenseResponse>(
            $"/api/hostels/{hostelOfA}/expenses/{expenseOfA.Id}", TestJson.Options);
        Assert.Equal(1000m, stillOriginal!.Amount);
    }

    private static async Task<ExpenseResponse> CreateExpenseAsync(
        HttpClient client, Guid hostelId, ExpenseCategory category, decimal amount)
    {
        var response = await client.PostAsJsonAsync($"/api/hostels/{hostelId}/expenses",
            new CreateExpenseRequest(category, amount, new DateOnly(2026, 1, 10), null), TestJson.Options);
        return (await response.Content.ReadFromJsonAsync<ExpenseResponse>(TestJson.Options))!;
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
