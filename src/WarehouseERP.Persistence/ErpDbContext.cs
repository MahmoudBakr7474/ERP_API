using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.Identity;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Identity.Infrastructure;
using WarehouseERP.Inventory;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Persistence;

// One context over every module, so a posting that touches several modules stays in one
// transaction. Each module owns its entity configurations and reaches its own tables through
// Set<T>(). It derives from IdentityDbContext so ASP.NET Core Identity's mapping stays Microsoft's
// (Identity's configurations move it into the identity schema); other modules still only see
// DbContext. Audit stamping is AuditingSaveChangesInterceptor.
public sealed class ErpDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IDataProtectionKeyContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options) : base(options) { }

    /// <summary>The Data Protection key ring (anti-forgery tokens), shared by every instance of the API.</summary>
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(OrganizationModule).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(CatalogModule).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(IdentityModule).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(InventoryModule).Assembly);
        builder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys", "identity");

        // Foreign keys between modules are configured here, the only place that sees every module.
        // A user's branch and warehouse grants (Identity) point at Organization's structure; a branch or
        // warehouse someone is granted can't be deleted from under them.
        builder.Entity<UserBranchGrant>()
            .HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserWarehouseGrant>()
            .HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        ConfigureInventoryReferences(builder);
    }

    // Inventory's rows point at Organization's structure and Catalog's products. A location is always referenced
    // together with its warehouse (the (WarehouseId, Id) alternate key), so no row can name a location of another
    // warehouse. Master data is never deleted, so these keys restrict.
    private static void ConfigureInventoryReferences(ModelBuilder builder)
    {
        builder.Entity<StockDocument>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockDocumentLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockAllocation>(b =>
        {
            b.HasOne<WarehouseLocation>().WithMany()
                .HasForeignKey(x => new { x.FromWarehouseId, x.FromLocationId })
                .HasPrincipalKey(x => new { x.WarehouseId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany()
                .HasForeignKey(x => new { x.ToWarehouseId, x.ToLocationId })
                .HasPrincipalKey(x => new { x.WarehouseId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockMovement>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany()
                .HasForeignKey(x => new { x.WarehouseId, x.LocationId })
                .HasPrincipalKey(x => new { x.WarehouseId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockBalance>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany()
                .HasForeignKey(x => new { x.WarehouseId, x.LocationId })
                .HasPrincipalKey(x => new { x.WarehouseId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ProductWarehouseStock>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<InventoryCostState>()
            .HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<InventoryCostEntry>()
            .HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<DocumentNumberSequence>()
            .HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<OverReservationAlert>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });

        // Milestone B: a transfer names three warehouses and, once dispatched, its own location in the transit one.
        builder.Entity<StockTransfer>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.TransitWarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.SourceBranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.DestinationBranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany()
                .HasForeignKey(x => new { x.TransitWarehouseId, x.TransitLocationId })
                .HasPrincipalKey(x => new { x.WarehouseId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockTransferLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany()
                .HasForeignKey(x => new { x.SourceWarehouseId, x.SourceLocationId })
                .HasPrincipalKey(x => new { x.WarehouseId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockCount>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockCountScope>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany()
                .HasForeignKey(x => new { x.WarehouseId, x.LocationId })
                .HasPrincipalKey(x => new { x.WarehouseId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<StockCountLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany()
                .HasForeignKey(x => new { x.WarehouseId, x.LocationId })
                .HasPrincipalKey(x => new { x.WarehouseId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
