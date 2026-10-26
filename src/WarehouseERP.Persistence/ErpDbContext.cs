using Microsoft.EntityFrameworkCore;
using WarehouseERP.Domain.Common;
using WarehouseERP.Domain.Organization;
using WarehouseERP.Domain.Catalog;

namespace WarehouseERP.Persistence;

public sealed class ErpDbContext : DbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductUnit> ProductUnits => Set<ProductUnit>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<ExternalItemCode> ExternalItemCodes => Set<ExternalItemCode>();
    public DbSet<TaxCode> TaxCodes => Set<TaxCode>();
    public DbSet<TaxRate> TaxRates => Set<TaxRate>();
    public DbSet<ProductTaxAssignment> ProductTaxAssignments => Set<ProductTaxAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);
        // Single DbContext; per-module schemas keep boundaries clear.
        // No operational stock tables yet; they belong to later migrations.
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
