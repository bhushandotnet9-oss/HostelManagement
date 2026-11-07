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
}
