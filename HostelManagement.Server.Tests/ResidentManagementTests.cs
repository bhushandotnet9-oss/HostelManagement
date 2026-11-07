using System.Net;
using System.Net.Http.Json;
using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Models;

namespace HostelManagement.Server.Tests;

public class ResidentManagementTests : IAsyncLifetime
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

    private static async Task<Guid> CreateCotAsync(HttpClient client)
    {
        var hostelResponse = await client.PostAsJsonAsync("/api/hostels", new CreateHostelRequest("Hostel One", "Pune"));
        var hostel = await hostelResponse.Content.ReadFromJsonAsync<HostelResponse>();

        var roomResponse = await client.PostAsJsonAsync($"/api/hostels/{hostel!.Id}/rooms", new CreateRoomRequest("Room One"));
        var room = await roomResponse.Content.ReadFromJsonAsync<RoomResponse>();

        var cotResponse = await client.PostAsJsonAsync($"/api/rooms/{room!.Id}/cots", new CreateCotRequest("Cot One"));
        var cot = await cotResponse.Content.ReadFromJsonAsync<CotResponse>(TestJson.Options);
        return cot!.Id;
    }

    [Fact]
    public async Task Resident_missing_aadhaar_is_rejected()
    {
        var client = await AuthenticatedClientAsync("resident1@example.com");

        var response = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "", new DateOnly(2026, 1, 15)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Assigning_resident_to_vacant_cot_marks_it_occupied()
    {
        var client = await AuthenticatedClientAsync("resident2@example.com");
        var cotId = await CreateCotAsync(client);

        var residentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15)));
        var resident = await residentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        var assignResponse = await client.PostAsJsonAsync($"/api/residents/{resident!.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);

        var roomId = await GetRoomIdForCot(client, cotId);
        var cotsResponse = await client.GetAsync($"/api/rooms/{roomId}/cots");
        var cots = await cotsResponse.Content.ReadFromJsonAsync<List<CotResponse>>(TestJson.Options);
        Assert.Equal(CotStatus.Occupied, cots!.Single(c => c.Id == cotId).Status);
    }

    [Fact]
    public async Task Assigning_resident_to_already_occupied_cot_is_rejected()
    {
        var client = await AuthenticatedClientAsync("resident3@example.com");
        var cotId = await CreateCotAsync(client);

        var firstResidentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15)));
        var firstResident = await firstResidentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);
        await client.PostAsJsonAsync($"/api/residents/{firstResident!.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);

        var secondResidentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Another Resident", "8888888888", "432143214321", new DateOnly(2026, 2, 1)));
        var secondResident = await secondResidentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        var secondAssignResponse = await client.PostAsJsonAsync($"/api/residents/{secondResident!.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 2, 1)), TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, secondAssignResponse.StatusCode);
    }

    [Fact]
    public async Task Checkout_without_recorded_notice_is_rejected()
    {
        var client = await AuthenticatedClientAsync("resident4@example.com");
        var cotId = await CreateCotAsync(client);

        var residentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15)));
        var resident = await residentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);
        await client.PostAsJsonAsync($"/api/residents/{resident!.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);

        var checkoutResponse = await client.PostAsJsonAsync($"/api/residents/{resident.Id}/checkout",
            new CheckoutRequest(10000m));

        Assert.Equal(HttpStatusCode.BadRequest, checkoutResponse.StatusCode);
    }

    [Fact]
    public async Task Checkout_after_notice_marks_resident_checked_out_and_cot_vacant()
    {
        var client = await AuthenticatedClientAsync("resident5@example.com");
        var cotId = await CreateCotAsync(client);

        var residentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15)));
        var resident = await residentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);
        await client.PostAsJsonAsync($"/api/residents/{resident!.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);

        await client.PostAsJsonAsync($"/api/residents/{resident.Id}/notice",
            new RecordNoticeRequest(new DateOnly(2026, 6, 1), 30));

        var checkoutResponse = await client.PostAsJsonAsync($"/api/residents/{resident.Id}/checkout",
            new CheckoutRequest(10000m));
        var checkedOutResident = await checkoutResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, checkoutResponse.StatusCode);
        Assert.Equal(ResidentStatus.CheckedOut, checkedOutResident!.Status);

        var roomId = await GetRoomIdForCot(client, cotId);
        var cotsResponse = await client.GetAsync($"/api/rooms/{roomId}/cots");
        var cots = await cotsResponse.Content.ReadFromJsonAsync<List<CotResponse>>(TestJson.Options);
        Assert.Equal(CotStatus.Vacant, cots!.Single(c => c.Id == cotId).Status);
    }

    private static async Task<Guid> GetRoomIdForCot(HttpClient client, Guid cotId)
    {
        var hostelsResponse = await client.GetAsync("/api/hostels");
        var hostels = await hostelsResponse.Content.ReadFromJsonAsync<List<HostelResponse>>();
        foreach (var hostel in hostels!)
        {
            var roomsResponse = await client.GetAsync($"/api/hostels/{hostel.Id}/rooms");
            var rooms = await roomsResponse.Content.ReadFromJsonAsync<List<RoomResponse>>();
            foreach (var room in rooms!)
            {
                var cotsResponse = await client.GetAsync($"/api/rooms/{room.Id}/cots");
                var cots = await cotsResponse.Content.ReadFromJsonAsync<List<CotResponse>>(TestJson.Options);
                if (cots!.Any(c => c.Id == cotId))
                {
                    return room.Id;
                }
            }
        }

        throw new InvalidOperationException("Cot not found.");
    }
}
