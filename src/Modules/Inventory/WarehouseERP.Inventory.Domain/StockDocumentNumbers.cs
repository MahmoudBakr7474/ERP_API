using System.Globalization;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Domain;

/// <summary>
/// How numbers read: RCV-CAI-2026-000017 (type, branch code, fiscal year, sequence) for posted documents, and DR-000123,
/// TR-000123 and CT-000123 for drafts, transfers and counts before they have one.
/// </summary>
public static class StockDocumentNumbers
{
    public static string Prefix(StockDocumentType documentType) => documentType switch
    {
        StockDocumentType.OpeningBalance => "OPN",
        StockDocumentType.Receipt => "RCV",
        StockDocumentType.Issue => "ISS",
        StockDocumentType.Adjustment => "ADJ",
        StockDocumentType.Reversal => "REV",
        StockDocumentType.TransferDispatch => "TRF",
        StockDocumentType.TransferReceipt => "TRR",
        StockDocumentType.CountAdjustment => "CNT",
        StockDocumentType.PurchaseReceipt => "PRC",
        StockDocumentType.PurchaseReturn => "PRT",
        StockDocumentType.SalesDelivery => "SDL",
        StockDocumentType.SalesReturn => "SRT",
        StockDocumentType.PosSale => "PSL",
        StockDocumentType.PosReturn => "PSR",
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "Unknown document type."),
    };

    public static string Format(StockDocumentType documentType, string branchCode, short fiscalYear, int sequenceNumber) =>
        string.Create(CultureInfo.InvariantCulture, $"{Prefix(documentType)}-{branchCode}-{fiscalYear}-{sequenceNumber:D6}");

    public static string DraftReference(int draftNumber) =>
        string.Create(CultureInfo.InvariantCulture, $"DR-{draftNumber:D6}");

    public static string TransferReference(int draftNumber) =>
        string.Create(CultureInfo.InvariantCulture, $"TR-{draftNumber:D6}");

    public static string CountReference(int draftNumber) =>
        string.Create(CultureInfo.InvariantCulture, $"CT-{draftNumber:D6}");
}
