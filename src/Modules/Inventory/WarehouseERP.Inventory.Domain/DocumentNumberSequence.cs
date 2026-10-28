using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// The last number given per document type, branch and fiscal year. Advanced only inside a posting's transaction, so
/// a failed posting gives no number back and leaves no gap.
/// </summary>
public sealed class DocumentNumberSequence
{
    private DocumentNumberSequence() { } // EF Core

    public StockDocumentType DocumentType { get; private set; }
    public Guid BranchId { get; private set; }
    public short FiscalYear { get; private set; }
    public int LastNumber { get; private set; }
}
