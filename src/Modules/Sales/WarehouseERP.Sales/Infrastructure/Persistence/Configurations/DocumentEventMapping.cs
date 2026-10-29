using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WarehouseERP.Sales.Domain;

namespace WarehouseERP.Sales.Infrastructure.Persistence.Configurations;

// Every document type keeps its history in its own table of the same shape.
internal static class DocumentEventMapping
{
    public static void Map<TEvent>(EntityTypeBuilder<TEvent> b, string table) where TEvent : DocumentEvent
    {
        b.ToTable(table, "sales", t => t.HasCheckConstraint($"CK_{table}_Action", "[Action] BETWEEN 1 AND 15"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).UseIdentityColumn();
        b.Property(x => x.Action).HasConversion<byte>().HasColumnType("tinyint");
        b.Property(x => x.AtUtc).IsUtcTimestamp();
        b.Property(x => x.Note).HasMaxLength(500);
        b.HasIndex(x => x.DocumentId);
    }
}
