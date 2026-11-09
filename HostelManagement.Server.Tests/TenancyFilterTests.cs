using System.Net;
using System.Net.Http.Json;
using HostelManagement.Server.Controllers.Dtos;
using HostelManagement.Server.Data;
using HostelManagement.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HostelManagement.Server.Tests;

public class TenancyFilterTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory = new();

    public async Task InitializeAsync() => await _factory.InitializeDatabaseAsync();

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Query_with_no_explicit_org_filter_cannot_return_another_orgs_row()
    {
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Hostels.Add(new Hostel { Id = Guid.NewGuid(), OwnerOrgId = orgA, Name = "Org A Hostel", Location = "Pune" });
            db.Hostels.Add(new Hostel { Id = Guid.NewGuid(), OwnerOrgId = orgB, Name = "Org B Hostel", Location = "Pune" });
            await db.SaveChangesAsync();
        }

        _factory.OwnerContext.OwnerOrgId = orgA;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // No explicit .Where(OwnerOrgId == ...) here - the global query filter
            // must still scope this to org A alone.
            var hostels = await db.Hostels.ToListAsync();

            Assert.Single(hostels);
            Assert.Equal("Org A Hostel", hostels[0].Name);
        }
    }

    [Fact]
    public async Task No_authenticated_owner_sees_no_rows()
    {
        var orgA = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Hostels.Add(new Hostel { Id = Guid.NewGuid(), OwnerOrgId = orgA, Name = "Org A Hostel", Location = "Pune" });
            await db.SaveChangesAsync();
        }

        _factory.OwnerContext.OwnerOrgId = null;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hostels = await db.Hostels.ToListAsync();

            Assert.Empty(hostels);
        }
    }

    [Fact]
    public async Task Logging_a_deposit_payment_for_another_orgs_resident_is_not_found()
    {
        var orgB = await AuthenticatedClientAsync("tenancy-org-b@example.com");
        var residentId = await CreateAssignedResidentAsync(orgB);

        var orgA = await AuthenticatedClientAsync("tenancy-org-a@example.com");
        var response = await orgA.PostAsJsonAsync($"/api/residents/{residentId}/deposit-payments",
            new LogDepositPaymentRequest(1000m, new DateOnly(2026, 1, 1)));

        // 404 rather than 403: the query filter means org A's request genuinely
        // cannot see the row, so this is indistinguishable from a missing one.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Empty(await db.Payments.IgnoreQueryFilters().ToListAsync());
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

    private static async Task<Guid> CreateAssignedResidentAsync(HttpClient client)
    {
        var hostel = await (await client.PostAsJsonAsync("/api/hostels",
            new CreateHostelRequest("Hostel One", "Pune"))).Content.ReadFromJsonAsync<HostelResponse>();
        var room = await (await client.PostAsJsonAsync($"/api/hostels/{hostel!.Id}/rooms",
            new CreateRoomRequest("Room One"))).Content.ReadFromJsonAsync<RoomResponse>();
        var cot = await (await client.PostAsJsonAsync($"/api/rooms/{room!.Id}/cots",
            new CreateCotRequest("Cot One"))).Content.ReadFromJsonAsync<CotResponse>(TestJson.Options);
        var resident = await (await client.PostAsJsonAsync("/api/residents",
            new CreateResidentRequest("Ravi Kumar", "9999999999", "123412341234", new DateOnly(2026, 1, 1))))
            .Content.ReadFromJsonAsync<ResidentResponse>(TestJson.Options);

        await client.PostAsJsonAsync($"/api/residents/{resident!.Id}/assign-cot",
            new AssignCotRequest(cot!.Id, 5000m, 10000m, BillingCycle.Monthly, new DateOnly(2026, 1, 1)),
            TestJson.Options);

        return resident.Id;
    }
}
