using System.Globalization;
using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>
/// How POS numbers read, per terminal and fiscal year: CAI-T1-2026-000017 for a receipt, CAI-T1-R-2026-000003 for a
/// return, CAI-T1-SH-2026-000042 for a shift. Given inside the transaction that records the document, kept forever.
/// </summary>
public static class PosNumbers
{
    public static string Format(PosDocumentType documentType, string terminalCode, short fiscalYear, int sequenceNumber) =>
        documentType switch
        {
            PosDocumentType.Sale => string.Create(CultureInfo.InvariantCulture, $"{terminalCode}-{fiscalYear}-{sequenceNumber:D6}"),
            PosDocumentType.Return => string.Create(CultureInfo.InvariantCulture, $"{terminalCode}-R-{fiscalYear}-{sequenceNumber:D6}"),
            PosDocumentType.Shift => string.Create(CultureInfo.InvariantCulture, $"{terminalCode}-SH-{fiscalYear}-{sequenceNumber:D6}"),
            _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "Unknown document type."),
        };
}
