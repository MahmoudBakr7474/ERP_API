using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WarehouseERP.Identity.Infrastructure.Persistence.Configurations;

// Unused by the ERP today; kept in the schema because ASP.NET Core Identity maps them (external logins later).
internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("UserClaims", IdentityTables.Schema);
}
