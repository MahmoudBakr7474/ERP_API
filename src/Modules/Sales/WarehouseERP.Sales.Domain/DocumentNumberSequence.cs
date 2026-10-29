using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// The last number given in a series (document type, branch, fiscal year). Written only by the numbering SQL, which
/// locks the row until the numbering transaction ends, so a rolled-back document leaves no gap.
/// </summary>
public sealed class DocumentNumberSequence
{
    private DocumentNumberSequence() { } // EF Core

    public SalesDocumentType DocumentType { get; private set; }
    public Guid BranchId { get; private set; }
    public short FiscalYear { get; private set; }
    public int LastNumber { get; private set; }
}
