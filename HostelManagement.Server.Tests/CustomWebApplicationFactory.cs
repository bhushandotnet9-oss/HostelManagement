using HostelManagement.Server.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HostelManagement.Server.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"hostelmanagement-test-{Guid.NewGuid()}.db");

    public MutableOwnerContext OwnerContext { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));

            // HttpCurrentOwnerContext only reads ambient state (the singleton
            // IHttpContextAccessor), so it's safe to hold as a long-lived
            // singleton here even though production registers it Scoped.
            services.RemoveAll<ICurrentOwnerContext>();
            services.AddSingleton<HttpCurrentOwnerContext>();
            services.AddSingleton<ICurrentOwnerContext>(sp =>
                OwnerContext = new MutableOwnerContext(sp.GetRequiredService<HttpCurrentOwnerContext>()));
        });
    }

    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup of a temp file; not worth failing the test run over.
        }
    }
}
