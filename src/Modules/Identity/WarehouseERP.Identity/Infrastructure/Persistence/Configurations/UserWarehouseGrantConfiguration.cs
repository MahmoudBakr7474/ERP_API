using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Identity.Domain;

namespace WarehouseERP.Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserWarehouseGrantConfiguration : IEntityTypeConfiguration<UserWarehouseGrant>
{
    public void Configure(EntityTypeBuilder<UserWarehouseGrant> builder)
    {
        builder.ToTemporalTable("UserWarehouseAccess");
        builder.HasKey(x => new { x.UserAccessId, x.WarehouseId });
    }
}
