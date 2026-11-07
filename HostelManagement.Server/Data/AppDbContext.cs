using System.Reflection;
using HostelManagement.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace HostelManagement.Server.Data;

public class AppDbContext : DbContext
{
    private static readonly MethodInfo SetTenantFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentOwnerContext ownerContext)
        : base(options)
    {
        // Guid.Empty when there is no authenticated owner, so the filter below
        // matches no rows rather than accidentally matching an unscoped record.
        OwnerOrgIdForFilter = ownerContext.OwnerOrgId ?? Guid.Empty;
    }

    /// <summary>
    /// Referenced as `this.OwnerOrgIdForFilter` inside each HasQueryFilter lambda below.
    /// EF Core re-evaluates `this`-bound members per query against the executing
    /// context instance, so this stays correct even though the model (and the
    /// filter expressions built from it) is built once and cached per DbContext type.
    /// </summary>
    public Guid OwnerOrgIdForFilter { get; }

    public DbSet<OwnerOrg> OwnerOrgs => Set<OwnerOrg>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Hostel> Hostels => Set<Hostel>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Cot> Cots => Set<Cot>();
    public DbSet<Resident> Residents => Set<Resident>();
    public DbSet<DuePeriod> DuePeriods => Set<DuePeriod>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Expense> Expenses => Set<Expense>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (typeof(ITenantScoped).IsAssignableFrom(entityType.ClrType))
            {
                SetTenantFilterMethod.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
            }
        }

        modelBuilder.Entity<Owner>()
            .HasIndex(o => o.Email)
            .IsUnique();

        modelBuilder.Entity<Resident>()
            .HasOne(r => r.Cot)
            .WithOne(c => c!.CurrentResident)
            .HasForeignKey<Resident>(r => r.CotId);
    }

    // Invoked once per tenant-scoped entity type via reflection above; the `this`
    // reference inside the lambda is what makes the filter re-evaluate per request.
    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.OwnerOrgId == this.OwnerOrgIdForFilter);
    }

    public override int SaveChanges()
    {
        StampTenantIdOnAddedEntries();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenantIdOnAddedEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    // Centralizes OwnerOrgId assignment for newly added rows so controllers never
    // need to set it themselves and can't forget to - mirrors the read-side query
    // filter above so writes are scoped just as reliably as reads.
    private void StampTenantIdOnAddedEntries()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added && entry.Entity.OwnerOrgId == Guid.Empty)
            {
                entry.Entity.OwnerOrgId = OwnerOrgIdForFilter;
            }
        }
    }
}
