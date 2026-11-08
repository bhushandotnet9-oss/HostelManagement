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

    [Fact]
    public async Task Hostel_name_and_location_can_be_corrected()
    {
        var client = await AuthenticatedClientAsync("hostelupdate1@example.com");
        var hostel = await CreateHostelAsync(client);

        var response = await client.PutAsJsonAsync($"/api/hostels/{hostel.Id}",
            new UpdateHostelRequest("Hostel One Renamed", "Mumbai"));
        var updated = await response.Content.ReadFromJsonAsync<HostelResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Hostel One Renamed", updated!.Name);
        Assert.Equal("Mumbai", updated.Location);
    }

    [Fact]
    public async Task Hostel_update_with_blank_name_is_rejected()
    {
        var client = await AuthenticatedClientAsync("hostelupdate2@example.com");
        var hostel = await CreateHostelAsync(client);

        var response = await client.PutAsJsonAsync($"/api/hostels/{hostel.Id}",
            new UpdateHostelRequest("   ", "Pune"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Another_orgs_hostel_cannot_be_updated()
    {
        // Each /api/account/setup call creates its own OwnerOrg, so these two
        // clients are genuinely separate tenants.
        var ownerA = await AuthenticatedClientAsync("hostelupdate3a@example.com");
        var ownerB = await AuthenticatedClientAsync("hostelupdate3b@example.com");
        var hostelOfA = await CreateHostelAsync(ownerA);

        var response = await ownerB.PutAsJsonAsync($"/api/hostels/{hostelOfA.Id}",
            new UpdateHostelRequest("Hijacked", "Nowhere"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var stillOriginal = await ownerA.GetFromJsonAsync<HostelResponse>($"/api/hostels/{hostelOfA.Id}");
        Assert.Equal("Hostel One", stillOriginal!.Name);
    }

    [Fact]
    public async Task Empty_hostel_can_be_deleted()
    {
        var client = await AuthenticatedClientAsync("hosteldelete1@example.com");
        var hostel = await CreateHostelAsync(client);

        var response = await client.DeleteAsync($"/api/hostels/{hostel.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var hostels = await client.GetFromJsonAsync<List<HostelResponse>>("/api/hostels");
        Assert.DoesNotContain(hostels!, h => h.Id == hostel.Id);
    }

    [Fact]
    public async Task Hostel_with_rooms_cannot_be_deleted()
    {
        var client = await AuthenticatedClientAsync("hosteldelete2@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);

        var response = await client.DeleteAsync($"/api/hostels/{hostel.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var hostels = await client.GetFromJsonAsync<List<HostelResponse>>("/api/hostels");
        Assert.Contains(hostels!, h => h.Id == hostel.Id);
        var rooms = await client.GetFromJsonAsync<List<RoomResponse>>($"/api/hostels/{hostel.Id}/rooms");
        Assert.Contains(rooms!, r => r.Id == room.Id);
    }

    [Fact]
    public async Task Hostel_with_expenses_cannot_be_deleted()
    {
        var client = await AuthenticatedClientAsync("hosteldelete3@example.com");
        var hostel = await CreateHostelAsync(client);
        await client.PostAsJsonAsync($"/api/hostels/{hostel.Id}/expenses",
            new CreateExpenseRequest(ExpenseCategory.Wifi, 800m, new DateOnly(2026, 2, 1), null), TestJson.Options);

        var response = await client.DeleteAsync($"/api/hostels/{hostel.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var hostels = await client.GetFromJsonAsync<List<HostelResponse>>("/api/hostels");
        Assert.Contains(hostels!, h => h.Id == hostel.Id);
    }

    [Fact]
    public async Task Room_name_can_be_corrected()
    {
        var client = await AuthenticatedClientAsync("roomupdate1@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);

        var response = await client.PutAsJsonAsync($"/api/hostels/{hostel.Id}/rooms/{room.Id}",
            new UpdateRoomRequest("Room One Renamed"));
        var updated = await response.Content.ReadFromJsonAsync<RoomResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Room One Renamed", updated!.Name);
    }

    [Fact]
    public async Task Room_update_with_blank_name_is_rejected()
    {
        var client = await AuthenticatedClientAsync("roomupdate2@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);

        var response = await client.PutAsJsonAsync($"/api/hostels/{hostel.Id}/rooms/{room.Id}",
            new UpdateRoomRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Room_addressed_through_the_wrong_hostel_is_not_found()
    {
        var client = await AuthenticatedClientAsync("roomupdate3@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);
        var otherHostelResponse = await client.PostAsJsonAsync("/api/hostels", new CreateHostelRequest("Hostel Two", "Nashik"));
        var otherHostel = await otherHostelResponse.Content.ReadFromJsonAsync<HostelResponse>();

        var response = await client.PutAsJsonAsync($"/api/hostels/{otherHostel!.Id}/rooms/{room.Id}",
            new UpdateRoomRequest("Wrong Parent"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Empty_room_can_be_deleted()
    {
        var client = await AuthenticatedClientAsync("roomdelete1@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);

        var response = await client.DeleteAsync($"/api/hostels/{hostel.Id}/rooms/{room.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var rooms = await client.GetFromJsonAsync<List<RoomResponse>>($"/api/hostels/{hostel.Id}/rooms");
        Assert.DoesNotContain(rooms!, r => r.Id == room.Id);
    }

    [Fact]
    public async Task Room_with_cots_cannot_be_deleted()
    {
        var client = await AuthenticatedClientAsync("roomdelete2@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);
        await client.PostAsJsonAsync($"/api/rooms/{room.Id}/cots", new CreateCotRequest("Cot A"));

        var response = await client.DeleteAsync($"/api/hostels/{hostel.Id}/rooms/{room.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var rooms = await client.GetFromJsonAsync<List<RoomResponse>>($"/api/hostels/{hostel.Id}/rooms");
        Assert.Contains(rooms!, r => r.Id == room.Id);
        var cots = await client.GetFromJsonAsync<List<CotResponse>>($"/api/rooms/{room.Id}/cots", TestJson.Options);
        Assert.Single(cots!);
    }

    [Fact]
    public async Task Cot_label_can_be_corrected_without_changing_its_status()
    {
        var client = await AuthenticatedClientAsync("cotupdate1@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);
        var cot = await CreateCotAsync(client, room.Id, "Cot A");

        // Occupy the cot so the update has a non-default status to preserve.
        var residentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15)));
        var resident = await residentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);
        await client.PostAsJsonAsync($"/api/residents/{resident!.Id}/assign-cot",
            new AssignCotRequest(cot.Id, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);

        var response = await client.PutAsJsonAsync($"/api/rooms/{room.Id}/cots/{cot.Id}",
            new UpdateCotRequest("Cot A1"));
        var updated = await response.Content.ReadFromJsonAsync<CotResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Cot A1", updated!.Label);
        Assert.Equal(CotStatus.Occupied, updated.Status);
    }

    [Fact]
    public async Task Cot_update_with_blank_label_is_rejected()
    {
        var client = await AuthenticatedClientAsync("cotupdate2@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);
        var cot = await CreateCotAsync(client, room.Id, "Cot A");

        var response = await client.PutAsJsonAsync($"/api/rooms/{room.Id}/cots/{cot.Id}",
            new UpdateCotRequest(" "));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Cot_addressed_through_the_wrong_room_is_not_found()
    {
        var client = await AuthenticatedClientAsync("cotupdate3@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);
        var cot = await CreateCotAsync(client, room.Id, "Cot A");
        var otherRoomResponse = await client.PostAsJsonAsync($"/api/hostels/{hostel.Id}/rooms", new CreateRoomRequest("Room Two"));
        var otherRoom = await otherRoomResponse.Content.ReadFromJsonAsync<RoomResponse>();

        var response = await client.DeleteAsync($"/api/rooms/{otherRoom!.Id}/cots/{cot.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Vacant_cot_can_be_deleted_and_occupancy_total_drops()
    {
        var client = await AuthenticatedClientAsync("cotdelete1@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);
        await CreateCotAsync(client, room.Id, "Cot A");
        var cotToDelete = await CreateCotAsync(client, room.Id, "Cot B");

        var before = await client.GetFromJsonAsync<HostelOccupancyResponse>($"/api/hostels/{hostel.Id}/occupancy");
        var response = await client.DeleteAsync($"/api/rooms/{room.Id}/cots/{cotToDelete.Id}");
        var after = await client.GetFromJsonAsync<HostelOccupancyResponse>($"/api/hostels/{hostel.Id}/occupancy");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(before!.Total - 1, after!.Total);

        var cots = await client.GetFromJsonAsync<List<CotResponse>>($"/api/rooms/{room.Id}/cots", TestJson.Options);
        Assert.DoesNotContain(cots!, c => c.Id == cotToDelete.Id);
    }

    [Fact]
    public async Task Occupied_cot_cannot_be_deleted()
    {
        var client = await AuthenticatedClientAsync("cotdelete2@example.com");
        var hostel = await CreateHostelAsync(client);
        var room = await CreateRoomAsync(client, hostel.Id);
        var cot = await CreateCotAsync(client, room.Id, "Cot A");

        var residentResponse = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15)));
        var resident = await residentResponse.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);
        await client.PostAsJsonAsync($"/api/residents/{resident!.Id}/assign-cot",
            new AssignCotRequest(cot.Id, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);

        var response = await client.DeleteAsync($"/api/rooms/{room.Id}/cots/{cot.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var cots = await client.GetFromJsonAsync<List<CotResponse>>($"/api/rooms/{room.Id}/cots", TestJson.Options);
        Assert.Equal(CotStatus.Occupied, cots!.Single(c => c.Id == cot.Id).Status);

        var stillAssigned = await client.GetFromJsonAsync<ResidentResponse>($"/api/residents/{resident.Id}", TestJson.Options);
        Assert.Equal(cot.Id, stillAssigned!.CotId);
    }

    private static async Task<CotResponse> CreateCotAsync(HttpClient client, Guid roomId, string label)
    {
        var response = await client.PostAsJsonAsync($"/api/rooms/{roomId}/cots", new CreateCotRequest(label));
        return (await response.Content.ReadFromJsonAsync<CotResponse>(TestJson.Options))!;
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
