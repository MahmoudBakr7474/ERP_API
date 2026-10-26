using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog;
using WarehouseERP.Organization;

namespace WarehouseERP.Persistence;

// One context over every module, so a posting that touches several modules stays in one
// transaction. It has no DbSet properties: each module owns its entity configurations and
// reaches its own tables through Set<T>(). Audit stamping is AuditingSaveChangesInterceptor.
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
}
