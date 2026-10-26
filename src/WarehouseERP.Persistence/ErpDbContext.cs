using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog;
using WarehouseERP.Organization;
using WarehouseERP.SharedKernel.Domain;

namespace WarehouseERP.Persistence;

// One context over every module, so a posting that touches several modules stays in one
// transaction. It has no DbSet properties: each module owns its entity configurations and
// reaches its own tables through Set<T>().
public sealed class ErpDbContext : DbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationModule).Assembly);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogModule).Assembly);
        // Foreign keys between modules (e.g. Inventory -> Product) are configured here,
        // the only place that sees every module.
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampMasterData();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampMasterData();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void StampMasterData()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<MasterEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                // Preserve historical creation metadata.
                entry.Property(x => x.CreatedAtUtc).IsModified = false;
                entry.Property(x => x.CreatedByUserId).IsModified = false;
                entry.Entity.UpdatedAtUtc = now;
            }
        }
    }
}
