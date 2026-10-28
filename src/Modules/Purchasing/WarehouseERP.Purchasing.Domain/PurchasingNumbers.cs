using System.Globalization;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Domain;

/// <summary>
/// How numbers read: PO-CAI-2026-000017 (type, branch code, fiscal year, sequence), given when a document is approved
/// or posted and kept forever; DPO-000123 and the like for drafts before they have one.
/// </summary>
public static class PurchasingNumbers
{
    public static string Prefix(PurchasingDocumentType documentType) => documentType switch
    {
        PurchasingDocumentType.PurchaseRequest => "PR",
        PurchasingDocumentType.PurchaseOrder => "PO",
        PurchasingDocumentType.GoodsReceipt => "GRN",
        PurchasingDocumentType.SupplierInvoice => "SIN",
        PurchasingDocumentType.SupplierReturn => "SRN",
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "Unknown document type."),
    };

    public static string Format(PurchasingDocumentType documentType, string branchCode, short fiscalYear, int sequenceNumber) =>
        string.Create(CultureInfo.InvariantCulture, $"{Prefix(documentType)}-{branchCode}-{fiscalYear}-{sequenceNumber:D6}");

    public static string DraftReference(PurchasingDocumentType documentType, int draftNumber) =>
        string.Create(CultureInfo.InvariantCulture, $"D{Prefix(documentType)}-{draftNumber:D6}");
}
