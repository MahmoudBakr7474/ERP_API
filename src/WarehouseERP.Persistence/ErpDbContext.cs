using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Catalog;
using WarehouseERP.Identity;
using WarehouseERP.Identity.Domain;
using WarehouseERP.Identity.Infrastructure;
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
        builder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys", "identity");

        // Foreign keys between modules are configured here, the only place that sees every module.
        // A user's branch and warehouse grants (Identity) point at Organization's structure; a branch or
        // warehouse someone is granted can't be deleted from under them.
        builder.Entity<UserBranchGrant>()
            .HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<UserWarehouseGrant>()
            .HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
