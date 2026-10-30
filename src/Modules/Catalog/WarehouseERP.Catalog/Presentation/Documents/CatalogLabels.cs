using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Presentation.Documents;

// Catalog words as documents print them, the same terms as the web app's translations.
internal static class CatalogLabels
{
    public static string Type(DocumentContext c, ProductType type) => type switch
    {
        ProductType.StockItem => c.L("صنف مخزني", "Stock item"),
        ProductType.NonStockItem => c.L("صنف غير مخزني", "Non-stock item"),
        _ => c.L("خدمة", "Service"),
    };

    public static string Tracking(DocumentContext c, TrackingMode tracking) => tracking switch
    {
        TrackingMode.Batch => c.L("تشغيلة", "Batch"),
        TrackingMode.Serial => c.L("سيريال", "Serial"),
        _ => c.L("بدون تتبع", "None"),
    };

    public static string Barcode(DocumentContext c, BarcodeType type) => type switch
    {
        BarcodeType.Ean13 => "EAN-13",
        BarcodeType.Gtin => "GTIN",
        BarcodeType.Other => c.L("آخر", "Other"),
        _ => c.L("داخلي", "Internal"),
    };

    public static string Issue(DocumentContext c, CatalogDataIssue issue) => issue switch
    {
        CatalogDataIssue.NoBarcode => c.L("بدون باركود", "No barcode"),
        CatalogDataIssue.NoCategory => c.L("بدون تصنيف", "No category"),
        CatalogDataIssue.InactiveCategory => c.L("تصنيف غير نشط", "Inactive category"),
        CatalogDataIssue.InactiveBaseUnit => c.L("وحدة أساسية غير نشطة", "Inactive base unit"),
        CatalogDataIssue.InactiveUnitInUse => c.L("وحدة غير نشطة مستخدمة", "Inactive unit in use"),
        CatalogDataIssue.FractionalFactorOnWholeUnit => c.L("معامل كسري لوحدة لا تقبل الكسور", "Fractional factor on a whole unit"),
        _ => c.L("بدون اسم إنجليزي", "No English name"),
    };

    public static string Status(DocumentContext c, bool isActive) => isActive ? c.L("نشط", "Active") : c.L("غير نشط", "Inactive");

    /// <summary>"Active", "Inactive" or "All" for a status filter.</summary>
    public static string StatusFilter(DocumentContext c, bool? isActive) =>
        isActive is { } active ? Status(c, active) : c.L("الكل", "All");
}
