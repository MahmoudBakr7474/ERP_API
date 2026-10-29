using System.Globalization;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Domain;

/// <summary>
/// How numbers read: SO-CAI-2026-000017 (type, branch code, fiscal year, sequence), given when a document is issued,
/// confirmed, posted or approved and kept forever; DSO-000123 and the like for drafts before they have one.
/// </summary>
public static class SalesNumbers
{
    public static string Prefix(SalesDocumentType documentType) => documentType switch
    {
        SalesDocumentType.Quotation => "QT",
        SalesDocumentType.Order => "SO",
        SalesDocumentType.Delivery => "DN",
        SalesDocumentType.Invoice => "INV",
        SalesDocumentType.Return => "SR",
        _ => throw new ArgumentOutOfRangeException(nameof(documentType), documentType, "Unknown document type."),
    };

    public static string Format(SalesDocumentType documentType, string branchCode, short fiscalYear, int sequenceNumber) =>
        string.Create(CultureInfo.InvariantCulture, $"{Prefix(documentType)}-{branchCode}-{fiscalYear}-{sequenceNumber:D6}");

    public static string DraftReference(SalesDocumentType documentType, int draftNumber) =>
        string.Create(CultureInfo.InvariantCulture, $"D{Prefix(documentType)}-{draftNumber:D6}");
}
