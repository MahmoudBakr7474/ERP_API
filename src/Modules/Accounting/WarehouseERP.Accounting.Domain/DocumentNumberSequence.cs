using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Domain;

/// <summary>The last number taken in one journal type's series in one fiscal year; advanced only by locked SQL.</summary>
public sealed class DocumentNumberSequence
{
    private DocumentNumberSequence() { } // EF Core

    public JournalType JournalType { get; private set; }
    public Guid FiscalYearId { get; private set; }
    public int LastNumber { get; private set; }
}
