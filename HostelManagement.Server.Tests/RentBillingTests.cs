using System.Net;
using System.Net.Http.Json;
using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Models;

namespace HostelManagement.Server.Tests;

public class RentBillingTests : IAsyncLifetime
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

    private static async Task<(Guid ResidentId, Guid CotId)> CreateAssignedResidentAsync(
        HttpClient client, DateOnly moveInDate, decimal rentAmount, BillingCycle billingCycle)
    {
        var hostelResponse = await client.PostAsJsonAsync("/api/hostels", new CreateHostelRequest("Hostel One", "Pune"));
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

        return (resident.Id, cot.Id);
    }

    [Fact]
    public async Task No_due_periods_are_generated_for_a_checked_out_resident()
    {
        var client = await AuthenticatedClientAsync("billing1@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        await client.PostAsJsonAsync($"/api/residents/{residentId}/notice",
            new RecordNoticeRequest(new DateOnly(2026, 1, 20), 30));
        await client.PostAsJsonAsync($"/api/residents/{residentId}/checkout", new CheckoutRequest(10000m));

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, duesResponse.StatusCode);
        Assert.Empty(dues!);
    }

    [Fact]
    public async Task Partial_payment_leaves_the_due_period_unpaid()
    {
        var client = await AuthenticatedClientAsync("billing2@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);
        var firstDue = dues!.First();

        var paymentResponse = await client.PostAsJsonAsync($"/api/dues/{firstDue.Id}/payments",
            new LogPaymentRequest(2000m, new DateOnly(2026, 1, 5)));
        var updated = await paymentResponse.Content.ReadFromJsonAsync<DuePeriodResponse>(TestJson.Options);

        Assert.False(updated!.IsPaid);
        Assert.Equal(2000m, updated.AmountPaid);
    }

    [Fact]
    public async Task Payment_covering_full_rent_marks_the_due_period_paid()
    {
        var client = await AuthenticatedClientAsync("billing3@example.com");
        var (residentId, _) = await CreateAssignedResidentAsync(
            client, new DateOnly(2026, 1, 1), 5000m, BillingCycle.Monthly);

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);
        var firstDue = dues!.First();

        var paymentResponse = await client.PostAsJsonAsync($"/api/dues/{firstDue.Id}/payments",
            new LogPaymentRequest(5000m, new DateOnly(2026, 1, 5)));
        var updated = await paymentResponse.Content.ReadFromJsonAsync<DuePeriodResponse>(TestJson.Options);

        Assert.True(updated!.IsPaid);
    }

    [Fact]
    public async Task Unpaid_due_period_past_its_due_date_is_marked_overdue()
    {
        var client = await AuthenticatedClientAsync("billing4@example.com");
        var pastMoveIn = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-2));
        var (residentId, _) = await CreateAssignedResidentAsync(client, pastMoveIn, 5000m, BillingCycle.Monthly);

        var duesResponse = await client.GetAsync($"/api/residents/{residentId}/dues");
        var dues = await duesResponse.Content.ReadFromJsonAsync<List<DuePeriodResponse>>(TestJson.Options);

        Assert.True(dues!.First().IsOverdue);
    }
}
