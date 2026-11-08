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

    [Fact]
    public async Task Resident_kyc_details_can_be_corrected()
    {
        var client = await AuthenticatedClientAsync("residentupdate1@example.com");
        var resident = await CreateResidentAsync(client);

        var response = await client.PutAsJsonAsync($"/api/residents/{resident.Id}",
            new UpdateResidentRequest("Ravi Kumar", "8888888888", "123412341234", new DateOnly(2026, 1, 15), null, null, null),
            TestJson.Options);
        var updated = await response.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("8888888888", updated!.ContactNumber);
    }

    [Fact]
    public async Task Resident_update_with_blank_aadhaar_is_rejected()
    {
        var client = await AuthenticatedClientAsync("residentupdate2@example.com");
        var resident = await CreateResidentAsync(client);

        var response = await client.PutAsJsonAsync($"/api/residents/{resident.Id}",
            new UpdateResidentRequest("Ravi Kumar", "9999999999", "", new DateOnly(2026, 1, 15), null, null, null),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Another_orgs_resident_cannot_be_updated()
    {
        var ownerA = await AuthenticatedClientAsync("residentupdate3a@example.com");
        var ownerB = await AuthenticatedClientAsync("residentupdate3b@example.com");
        var residentOfA = await CreateResidentAsync(ownerA);

        var response = await ownerB.PutAsJsonAsync($"/api/residents/{residentOfA.Id}",
            new UpdateResidentRequest("Hijacked", "0000000000", "999999999999", new DateOnly(2026, 1, 15), null, null, null),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var stillOriginal = await ownerA.GetFromJsonAsync<ResidentResponse>($"/api/residents/{residentOfA.Id}", TestJson.Options);
        Assert.Equal("Ravi Kumar", stillOriginal!.Name);
    }

    [Fact]
    public async Task Assignment_terms_can_be_corrected_for_an_assigned_resident()
    {
        var client = await AuthenticatedClientAsync("residentupdate4@example.com");
        var cotId = await CreateCotAsync(client);
        var resident = await CreateResidentAsync(client);
        await client.PostAsJsonAsync($"/api/residents/{resident.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);

        var response = await client.PutAsJsonAsync($"/api/residents/{resident.Id}",
            new UpdateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15),
                6000m, 12000m, BillingCycle.Quarterly),
            TestJson.Options);
        var updated = await response.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(6000m, updated!.RentAmount);
        Assert.Equal(12000m, updated.DepositAmount);
        Assert.Equal(BillingCycle.Quarterly, updated.BillingCycle);
    }

    [Fact]
    public async Task Terms_cannot_be_set_for_an_unassigned_resident()
    {
        var client = await AuthenticatedClientAsync("residentupdate5@example.com");
        var resident = await CreateResidentAsync(client);

        var response = await client.PutAsJsonAsync($"/api/residents/{resident.Id}",
            new UpdateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15),
                6000m, null, null),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Correction_leaves_a_serving_notice_residents_lifecycle_fields_unchanged()
    {
        var client = await AuthenticatedClientAsync("residentupdate6@example.com");
        var cotId = await CreateCotAsync(client);
        var resident = await CreateResidentAsync(client);
        await client.PostAsJsonAsync($"/api/residents/{resident.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);
        await client.PostAsJsonAsync($"/api/residents/{resident.Id}/notice",
            new RecordNoticeRequest(new DateOnly(2026, 6, 1), 30));

        var response = await client.PutAsJsonAsync($"/api/residents/{resident.Id}",
            new UpdateResidentRequest("Ravi K Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15), null, null, null),
            TestJson.Options);
        var updated = await response.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Ravi K Kumar", updated!.Name);
        Assert.Equal(ResidentStatus.ServingNotice, updated.Status);
        Assert.Equal(cotId, updated.CotId);
        Assert.Equal(new DateOnly(2026, 6, 1), updated.NoticeDate);
        Assert.Equal(30, updated.NoticePeriodDays);
        Assert.Null(updated.CheckoutDate);
        Assert.Null(updated.DepositRefundAmount);
    }

    [Fact]
    public async Task Move_in_date_can_be_corrected_before_billing_starts()
    {
        var client = await AuthenticatedClientAsync("residentupdate7@example.com");
        var resident = await CreateResidentAsync(client);

        var response = await client.PutAsJsonAsync($"/api/residents/{resident.Id}",
            new UpdateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 2, 1), null, null, null),
            TestJson.Options);
        var updated = await response.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new DateOnly(2026, 2, 1), updated!.MoveInDate);
    }

    [Fact]
    public async Task Move_in_date_cannot_be_changed_once_billing_has_started()
    {
        var client = await AuthenticatedClientAsync("residentupdate8@example.com");
        var (resident, moveInDate) = await MaterializedResidentAsync(client);

        var response = await client.PutAsJsonAsync($"/api/residents/{resident.Id}",
            new UpdateResidentRequest("Ravi Kumar", "9999999999", "123412341234", moveInDate.AddDays(5), null, null, null),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var unchanged = await client.GetFromJsonAsync<ResidentResponse>($"/api/residents/{resident.Id}", TestJson.Options);
        Assert.Equal(moveInDate, unchanged!.MoveInDate);
    }

    [Fact]
    public async Task Other_fields_are_still_correctable_once_billing_has_started()
    {
        var client = await AuthenticatedClientAsync("residentupdate9@example.com");
        var (resident, moveInDate) = await MaterializedResidentAsync(client);

        var response = await client.PutAsJsonAsync($"/api/residents/{resident.Id}",
            new UpdateResidentRequest("Ravi Kumar", "7777777777", "123412341234", moveInDate, null, null, null),
            TestJson.Options);
        var updated = await response.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("7777777777", updated!.ContactNumber);
    }

    [Fact]
    public async Task Rent_correction_leaves_existing_due_periods_untouched()
    {
        var client = await AuthenticatedClientAsync("residentupdate10@example.com");
        var (resident, moveInDate) = await MaterializedResidentAsync(client);

        var before = await client.GetFromJsonAsync<List<DuePeriodResponse>>($"/api/residents/{resident.Id}/dues", TestJson.Options);
        Assert.NotEmpty(before!);
        Assert.All(before!, d => Assert.Equal(5000m, d.RentAmount));

        await client.PutAsJsonAsync($"/api/residents/{resident.Id}",
            new UpdateResidentRequest("Ravi Kumar", "9999999999", "123412341234", moveInDate, 9000m, null, null),
            TestJson.Options);

        // Every stored period is already due, so all of them keep the old amount;
        // the new rent only shows up on periods materialized from here on.
        var after = await client.GetFromJsonAsync<List<DuePeriodResponse>>($"/api/residents/{resident.Id}/dues", TestJson.Options);
        Assert.Equal(before!.Count, after!.Count);
        Assert.All(after!, d => Assert.Equal(5000m, d.RentAmount));

        var updated = await client.GetFromJsonAsync<ResidentResponse>($"/api/residents/{resident.Id}", TestJson.Options);
        Assert.Equal(9000m, updated!.RentAmount);
    }

    [Fact]
    public async Task Resident_without_payments_can_be_deleted_and_frees_the_cot()
    {
        var client = await AuthenticatedClientAsync("residentdelete1@example.com");
        var cotId = await CreateCotAsync(client);
        var resident = await CreateResidentAsync(client);
        await client.PostAsJsonAsync($"/api/residents/{resident.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);
        // Materialize dues so the delete has derived rows to clean up.
        await client.GetAsync($"/api/residents/{resident.Id}/dues");

        var response = await client.DeleteAsync($"/api/residents/{resident.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var residents = await client.GetFromJsonAsync<List<ResidentResponse>>("/api/residents", TestJson.Options);
        Assert.DoesNotContain(residents!, r => r.Id == resident.Id);

        var roomId = await GetRoomIdForCot(client, cotId);
        var cots = await client.GetFromJsonAsync<List<CotResponse>>($"/api/rooms/{roomId}/cots", TestJson.Options);
        Assert.Equal(CotStatus.Vacant, cots!.Single(c => c.Id == cotId).Status);

        var duesResponse = await client.GetAsync($"/api/residents/{resident.Id}/dues");
        Assert.Equal(HttpStatusCode.NotFound, duesResponse.StatusCode);
    }

    [Fact]
    public async Task Resident_with_a_recorded_payment_cannot_be_deleted()
    {
        var client = await AuthenticatedClientAsync("residentdelete2@example.com");
        var cotId = await CreateCotAsync(client);
        var resident = await CreateResidentAsync(client);
        await client.PostAsJsonAsync($"/api/residents/{resident.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 15)), TestJson.Options);

        var dues = await client.GetFromJsonAsync<List<DuePeriodResponse>>($"/api/residents/{resident.Id}/dues", TestJson.Options);
        await client.PostAsJsonAsync($"/api/dues/{dues!.First().Id}/payments",
            new LogPaymentRequest(1000m, new DateOnly(2026, 2, 1)));

        var response = await client.DeleteAsync($"/api/residents/{resident.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var residents = await client.GetFromJsonAsync<List<ResidentResponse>>("/api/residents", TestJson.Options);
        Assert.Contains(residents!, r => r.Id == resident.Id);

        var remainingDues = await client.GetFromJsonAsync<List<DuePeriodResponse>>($"/api/residents/{resident.Id}/dues", TestJson.Options);
        Assert.Equal(dues.Count, remainingDues!.Count);

        var roomId = await GetRoomIdForCot(client, cotId);
        var cots = await client.GetFromJsonAsync<List<CotResponse>>($"/api/rooms/{roomId}/cots", TestJson.Options);
        Assert.Equal(CotStatus.Occupied, cots!.Single(c => c.Id == cotId).Status);
    }

    private static async Task<ResidentResponse> CreateResidentAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 15)));
        return (await response.Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options))!;
    }

    /// <summary>
    /// A resident assigned to a cot with a move-in date far enough in the past
    /// that reading their dues materializes at least one DuePeriod.
    /// </summary>
    private static async Task<(ResidentResponse Resident, DateOnly MoveInDate)> MaterializedResidentAsync(HttpClient client)
    {
        var moveInDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-3);
        var cotId = await CreateCotAsync(client);
        var resident = await CreateResidentAsync(client);
        await client.PostAsJsonAsync($"/api/residents/{resident.Id}/assign-cot",
            new AssignCotRequest(cotId, 5000m, 10000m, BillingCycle.Monthly, moveInDate), TestJson.Options);
        await client.GetAsync($"/api/residents/{resident.Id}/dues");
        return (resident, moveInDate);
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
