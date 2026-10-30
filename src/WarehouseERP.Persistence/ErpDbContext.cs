using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.Catalog;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.Identity;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Identity.Infrastructure;
using WarehouseERP.Inventory;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization;
using WarehouseERP.Organization.Domain;
using WarehouseERP.Pos;
using WarehouseERP.Pos.Domain;
using WarehouseERP.Purchasing;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.Sales;
using WarehouseERP.Sales.Domain;

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
        builder.ApplyConfigurationsFromAssembly(typeof(PurchasingModule).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(SalesModule).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(PosModule).Assembly);
        builder.ApplyConfigurationsFromAssembly(typeof(AccountingModule).Assembly);
        builder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys", "identity");

        // Foreign keys between modules are configured here, the only place that sees every module.
        // A user's branch and warehouse grants (Identity) point at Organization's structure; a branch or
        // warehouse someone is granted can't be deleted from under them.
        builder.Entity<UserBranchGrant>()
            .HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserWarehouseGrant>()
            .HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        ConfigureInventoryReferences(builder);
        ConfigurePurchasingReferences(builder);
        ConfigureSalesReferences(builder);
        ConfigurePosReferences(builder);
        ConfigureAccountingReferences(builder);
    }

    // Every journal line names one of Organization's branches. Restrict: a branch with ledger lines is never deleted.
    private static void ConfigureAccountingReferences(ModelBuilder builder) =>
        builder.Entity<JournalLine>()
            .HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);

    // POS's rows point at Organization's structure, Catalog's products and units, Sales' customers, and the stock documents
    // and lines Inventory posted for its sales and returns (which name them back through SourceDocumentId). Restrict:
    // nothing they name is ever deleted.
    private static void ConfigurePosReferences(ModelBuilder builder)
    {
        builder.Entity<PosTerminal>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.SellingLocationId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PosShift>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PosSale>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.StockDocumentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PosSaleLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocumentLine>().WithMany().HasForeignKey(x => x.StockLineId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PosReturn>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.StockDocumentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PosReturnLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocumentLine>().WithMany().HasForeignKey(x => x.StockLineId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocumentLine>().WithMany().HasForeignKey(x => x.SoldStockLineId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PosCardIncident>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    // Sales's rows point at Organization's structure and address lookups, Catalog's products and units, and the stock
    // documents and lines Inventory posted for its deliveries and returns (which name them back through
    // SourceDocumentId). Restrict: nothing they name is ever deleted.
    private static void ConfigureSalesReferences(ModelBuilder builder)
    {
        builder.Entity<Customer>()
            .HasOne<Country>().WithMany().HasForeignKey(x => x.CountryCode).HasPrincipalKey(x => x.Code)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CustomerAddress>()
            .HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalesPrice>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<ProductTax>()
            .HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalesQuotation>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesQuotationLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesOrder>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesOrderLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesDelivery>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.StockDocumentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesDeliveryLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocumentLine>().WithMany().HasForeignKey(x => x.StockLineId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesInvoice>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesInvoiceLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesReturn>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.StockDocumentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SalesReturnLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocumentLine>().WithMany().HasForeignKey(x => x.StockLineId).OnDelete(DeleteBehavior.Restrict);
        });
        // Named apart from Inventory's and Purchasing's series tables, whose foreign keys would otherwise share the name.
        builder.Entity<global::WarehouseERP.Sales.Domain.DocumentNumberSequence>()
            .HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_SalesNumberSequences_Branches_BranchId");
    }

    // Purchasing's rows point at Organization's structure, Catalog's products and units, and the stock documents Inventory
    // posted for its receipts and returns (which name them back through SourceDocumentId). Restrict: nothing they name is
    // ever deleted.
    private static void ConfigurePurchasingReferences(ModelBuilder builder)
    {
        builder.Entity<PurchaseRequest>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PurchaseRequestLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PurchaseOrder>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<PurchaseOrderLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Unit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<GoodsReceipt>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.StockDocumentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<GoodsReceiptLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        });
        // A supplier's country (by its ISO code) and its addresses' cities are Organization's lookups.
        builder.Entity<Supplier>()
            .HasOne<Country>().WithMany().HasForeignKey(x => x.CountryCode).HasPrincipalKey(x => x.Code)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SupplierAddress>()
            .HasOne<City>().WithMany().HasForeignKey(x => x.CityId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SupplierInvoice>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SupplierInvoiceLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SupplierReturn>(b =>
        {
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<StockDocument>().WithMany().HasForeignKey(x => x.StockDocumentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<SupplierReturnLine>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<ProductUnit>().WithMany().HasForeignKey(x => x.ProductUnitId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<WarehouseLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<global::WarehouseERP.Inventory.Domain.DocumentNumberSequence>()
            .HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
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
        // Named apart from Inventory's series table, whose foreign key would otherwise have the same name.
        builder.Entity<global::WarehouseERP.Purchasing.Domain.DocumentNumberSequence>()
            .HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PurchasingNumberSequences_Branches_BranchId");
        builder.Entity<OverReservationAlert>(b =>
        {
            b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });
        // Sales: a sales order line's reservation of a product in a warehouse.
        builder.Entity<StockReservation>(b =>
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
