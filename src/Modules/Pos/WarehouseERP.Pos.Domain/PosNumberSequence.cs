using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// The last number given in a series (document type, terminal, fiscal year). Written only by the numbering SQL, which
/// locks the row until the numbering transaction ends, so a rolled-back sale leaves no gap.
/// </summary>
public sealed class PosNumberSequence
{
    private PosNumberSequence() { } // EF Core

    public PosDocumentType DocumentType { get; private set; }
    public Guid TerminalId { get; private set; }
    public short FiscalYear { get; private set; }
    public int LastNumber { get; private set; }
}
