using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Infrastructure.Persistence.Configurations;

internal sealed class FiscalYearEventConfiguration : IEntityTypeConfiguration<FiscalYearEvent>
{
    public void Configure(EntityTypeBuilder<FiscalYearEvent> b)
    {
        b.ToTable("FiscalYearEvents", "accounting",
            t => t.HasCheckConstraint("CK_FiscalYearEvents_Action", $"[Action] BETWEEN 1 AND {Enum.GetValues<FiscalYearAction>().Length}"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Action).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.AtUtc).IsUtcTimestamp();
        b.Property(x => x.Note).HasMaxLength(FiscalYear.MaxNoteLength);
        b.HasIndex(x => x.FiscalYearId);
    }
}
