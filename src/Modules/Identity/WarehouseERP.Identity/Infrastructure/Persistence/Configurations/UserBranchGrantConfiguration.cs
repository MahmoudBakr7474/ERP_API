using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserBranchGrantConfiguration : IEntityTypeConfiguration<UserBranchGrant>
{
    public void Configure(EntityTypeBuilder<UserBranchGrant> builder)
    {
        builder.ToTemporalTable("UserBranchAccess");
        builder.HasKey(x => new { x.UserAccessId, x.BranchId });
    }
}
