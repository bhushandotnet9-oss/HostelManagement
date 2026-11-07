using System.Net;
using System.Net.Http.Json;
using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Models;

namespace HostelManagement.Server.Tests;

public class HostelInventoryTests : IAsyncLifetime
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

    [Fact]
    public async Task Room_cannot_be_created_under_a_nonexistent_hostel()
    {
        var client = await AuthenticatedClientAsync("rooms1@example.com");

        var response = await client.PostAsJsonAsync($"/api/hostels/{Guid.NewGuid()}/rooms", new CreateRoomRequest("Room A"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cot_cannot_be_created_under_a_nonexistent_room()
    {
        var client = await AuthenticatedClientAsync("cots1@example.com");

        var response = await client.PostAsJsonAsync($"/api/rooms/{Guid.NewGuid()}/cots", new CreateCotRequest("Cot A"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cot_defaults_to_vacant_on_creation()
    {
        var client = await AuthenticatedClientAsync("cots2@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);

        var response = await client.PostAsJsonAsync($"/api/rooms/{room.Id}/cots", new CreateCotRequest("Cot A"));
        var cot = await response.Content.ReadFromJsonAsync<CotResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(CotStatus.Vacant, cot!.Status);
    }

    [Fact]
    public async Task Occupancy_overview_returns_correct_counts_for_a_seeded_hostel()
    {
        var client = await AuthenticatedClientAsync("occupancy1@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);

        await client.PostAsJsonAsync($"/api/rooms/{room.Id}/cots", new CreateCotRequest("Cot A"));
        await client.PostAsJsonAsync($"/api/rooms/{room.Id}/cots", new CreateCotRequest("Cot B"));
        await client.PostAsJsonAsync($"/api/rooms/{room.Id}/cots", new CreateCotRequest("Cot C"));

        var response = await client.GetAsync($"/api/hostels/{hostel.Id}/occupancy");
        var occupancy = await response.Content.ReadFromJsonAsync<HostelOccupancyResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, occupancy!.Total);
        Assert.Equal(0, occupancy.Occupied);
        Assert.Equal(3, occupancy.Vacant);
    }

    private static async Task<HostelResponse> CreateHostelAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/hostels", new CreateHostelRequest("Hostel One", "Pune"));
        return (await response.Content.ReadFromJsonAsync<HostelResponse>())!;
    }

    private static async Task<RoomResponse> CreateRoomAsync(HttpClient client, Guid hostelId)
    {
        var response = await client.PostAsJsonAsync($"/api/hostels/{hostelId}/rooms", new CreateRoomRequest("Room One"));
        return (await response.Content.ReadFromJsonAsync<RoomResponse>())!;
    }
}
