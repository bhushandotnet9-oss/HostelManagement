using System.Net;
using System.Net.Http.Json;
using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Models;

namespace HostelManagement.Server.Tests;

public class OwnerDashboardTests : IAsyncLifetime
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

    private static async Task<(Guid HostelId, Guid ResidentId)> CreateAssignedResidentAsync(
        HttpClient client, DateOnly moveInDate, decimal rentAmount, BillingCycle billingCycle, string hostelName = "Hostel One")
    {
        var hostelResponse = await client.PostAsJsonAsync("/api/hostels", new CreateHostelRequest(hostelName, "Pune"));
        var hostel = await hostelResponse.Content.ReadFromJsonAsync<HostelResponse>();
        var roomResponse = await client.PostAsJsonAsync($"/api/hostels/{hostel!.Id}/rooms", new CreateRoomRequest("Room One"));
        var room = await roomResponse.Content.ReadFromJsonAsync<RoomResponse>();
        var cotResponse = await client.PostAsJsonAsync($"/api/rooms/{room!.Id}/cots", new CreateCotRequest("Cot One"));
        var cot = await cotResponse.Content.ReadFromJsonAsync<CotResponse>(TestJson.Options);

        var residentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", moveInDate));
        var resident = await residentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        await client.PostAsJsonAsync($"/api/residents/{resident!.Id}/assign-cot",
            new AssignCotRequest(cot!.Id, rentAmount, rentAmount * 2, billingCycle, moveInDate), TestJson.Options);

        return (hostel.Id, resident.Id);
    }

    [Fact]
    public async Task Overdue_residents_list_includes_a_seeded_overdue_resident()
    {
        var client = await AuthenticatedClientAsync("dash1@example.com");
        var pastMoveIn = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2));
        var (hostelId, residentId) = await CreateAssignedResidentAsync(client, pastMoveIn, 5000m, BillingCycle.Monthly);

        var response = await client.GetAsync("/api/dashboard/overdue");
        var overdue = await response.Content.ReadFromJsonAsync<List<OverdueResidentResponse>>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var entry = overdue!.Single(o => o.ResidentId == residentId);
        Assert.Equal(hostelId, entry.HostelId);
        Assert.True(entry.AmountOwed > 0);
        Assert.True(entry.DaysOverdue > 0);
    }

    [Fact]
    public async Task Upcoming_dues_includes_residents_just_inside_the_window_and_excludes_those_outside()
    {
        var client = await AuthenticatedClientAsync("dash2@example.com");
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var oneMonthAgo = today.AddMonths(-1);

        // Monthly cycle anchored 1 month minus 7 days ago -> next due date lands exactly 7 days from
        // today (the edge of a 7-day window, so it must be included).
        var (_, nearResidentId) = await CreateAssignedResidentAsync(
            client, oneMonthAgo.AddDays(7), 5000m, BillingCycle.Monthly, "Hostel Near");

        // Monthly cycle anchored 1 month minus 8 days ago -> next due date lands 8 days from today
        // (one day past the edge of the window, so it must be excluded).
        var (_, farResidentId) = await CreateAssignedResidentAsync(
            client, oneMonthAgo.AddDays(8), 5000m, BillingCycle.Monthly, "Hostel Far");

        var response = await client.GetAsync("/api/dashboard/upcoming-dues?days=7");
        var upcoming = await response.Content.ReadFromJsonAsync<List<UpcomingDueResponse>>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(upcoming!, u => u.ResidentId == nearResidentId);
        Assert.DoesNotContain(upcoming!, u => u.ResidentId == farResidentId);
    }

    [Fact]
    public async Task Occupancy_summary_matches_seeded_data_per_hostel_and_combined()
    {
        var client = await AuthenticatedClientAsync("dash3@example.com");
        await CreateAssignedResidentAsync(client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly, "Hostel A");

        var response = await client.GetAsync("/api/dashboard/occupancy");
        var summary = await response.Content.ReadFromJsonAsync<OccupancySummaryResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hostelSummary = summary!.PerHostel.Single();
        Assert.Equal(1, hostelSummary.Total);
        Assert.Equal(1, hostelSummary.Occupied);
        Assert.Equal(0, hostelSummary.Vacant);
        Assert.Equal(100.0, hostelSummary.OccupancyPercent);
        Assert.Equal(1, summary.Combined.Total);
        Assert.Equal(1, summary.Combined.Occupied);
    }

    [Fact]
    public async Task Revenue_summary_computes_expected_from_due_periods_and_collected_from_payments()
    {
        var client = await AuthenticatedClientAsync("dash4@example.com");
        var (hostelId, residentId) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);
        var januaryDue = dues!.Single(d => d.DueDate == new DateOnly(2026, 1, 1));
        await client.PostAsJsonAsync($"/api/dues/{januaryDue.Id}/payments", new LogPaymentRequest(5000m, new DateOnly(2026, 1, 2)));

        var response = await client.GetAsync($"/api/hostels/{hostelId}/revenue?from=2026-01-01&to=2026-01-31");
        var revenue = await response.Content.ReadFromJsonAsync<RevenueSummaryResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5000m, revenue!.Expected);
        Assert.Equal(5000m, revenue.Collected);
    }

    [Fact]
    public async Task Dashboard_expense_totals_are_consistent_with_the_expense_tracking_breakdown()
    {
        var client = await AuthenticatedClientAsync("dash5@example.com");
        var hostelResponse = await client.PostAsJsonAsync("/api/hostels", new CreateHostelRequest("Hostel One", "Pune"));
        var hostel = await hostelResponse.Content.ReadFromJsonAsync<HostelResponse>();

        await client.PostAsJsonAsync($"/api/hostels/{hostel!.Id}/expenses",
            new CreateExpenseRequest(ExpenseCategory.Electricity, 1200m, new DateOnly(2026, 1, 10), null), TestJson.Options);

        var dashboardResponse = await client.GetAsync("/api/dashboard/expenses?from=2026-01-01&to=2026-01-31");
        var dashboardTotals = await dashboardResponse.Content.ReadFromJsonAsync<List<HostelExpenseTotals>>(TestJson.Options);

        var directResponse = await client.GetAsync($"/api/hostels/{hostel.Id}/expenses/breakdown?from=2026-01-01&to=2026-01-31");
        var directTotals = await directResponse.Content.ReadFromJsonAsync<List<ExpenseCategoryTotal>>(TestJson.Options);

        var dashboardEntry = dashboardTotals!.Single(h => h.HostelId == hostel.Id);
        Assert.Equal(directTotals!.Single().Total, dashboardEntry.Totals.Single().Total);
    }
}
