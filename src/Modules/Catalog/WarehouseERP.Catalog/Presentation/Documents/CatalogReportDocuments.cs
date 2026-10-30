using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Presentation.Documents;

// The catalog reports as rows and columns. Excel and CSV get analysis-friendly columns (codes and names apart, both
// names, timestamps); PDF gets the columns that fit a printed page. Values come from the Application queries
// unchanged: nothing is calculated here.
internal static class CatalogReportDocuments
{
    public static TabularReport Products(
        DocumentContext c, IReadOnlyList<ProductMasterResponse> rows, ExportFormat format, IReadOnlyList<ReportFilter> filters)
    {
        var title = c.L("قائمة الأصناف الرئيسية", "Product master list");
        if (format == ExportFormat.Pdf)
            return new TabularReport(c.Language, "catalog-products", title,
                [
                    new(c.L("كود الصنف", "SKU"), ColumnKind.Code, 1.25f),
                    new(c.L("الاسم العربي", "Arabic name"), ColumnKind.Text, 2.6f),
                    new(c.L("الاسم الإنجليزي", "English name"), ColumnKind.Text, 2.2f),
                    new(c.L("التصنيف", "Category"), ColumnKind.Text, 1.6f),
                    new(c.L("النوع", "Type"), ColumnKind.Text, 1.1f),
                    new(c.L("التتبع", "Tracking"), ColumnKind.Text, 0.85f),
                    new(c.L("الوحدة الأساسية", "Base unit"), ColumnKind.Text, 1f),
                    new(c.L("الوحدات", "Units"), ColumnKind.Count, 0.6f),
                    new(c.L("الباركود", "Barcodes"), ColumnKind.Count, 0.7f),
                    new(c.L("الحالة", "Status"), ColumnKind.Text, 0.75f),
                ],
                rows.Select(p => Row(p.Sku, p.NameAr, p.NameEn, CategoryName(c, p), CatalogLabels.Type(c, p.Type),
                    CatalogLabels.Tracking(c, p.TrackingMode), c.Name(p.BaseUnitNameAr, p.BaseUnitNameEn), p.UnitCount,
                    p.BarcodeCount, CatalogLabels.Status(c, p.IsActive))).ToList(),
                filters, Landscape: true, Summary: StatusSummary(c, rows.Count(p => p.IsActive), rows.Count(p => !p.IsActive)));

        return new TabularReport(c.Language, "catalog-products", title,
            [
                new(c.L("كود الصنف", "SKU"), ColumnKind.Code),
                new(c.L("الاسم العربي", "Arabic name")),
                new(c.L("الاسم الإنجليزي", "English name")),
                new(c.L("كود التصنيف", "Category code"), ColumnKind.Code),
                new(c.L("التصنيف", "Category")),
                new(c.L("النوع", "Type")),
                new(c.L("التتبع", "Tracking")),
                new(c.L("كود الوحدة الأساسية", "Base unit code"), ColumnKind.Code),
                new(c.L("الوحدة الأساسية", "Base unit")),
                new(c.L("الوحدات النشطة", "Active units"), ColumnKind.Count),
                new(c.L("الباركود النشط", "Active barcodes"), ColumnKind.Count),
                new(c.L("الحالة", "Status")),
                new(c.L("تاريخ الإنشاء", "Created"), ColumnKind.DateTime),
            ],
            rows.Select(p => Row(p.Sku, p.NameAr, p.NameEn, p.CategoryCode, CategoryName(c, p), CatalogLabels.Type(c, p.Type),
                CatalogLabels.Tracking(c, p.TrackingMode), p.BaseUnitCode, c.Name(p.BaseUnitNameAr, p.BaseUnitNameEn),
                p.UnitCount, p.BarcodeCount, CatalogLabels.Status(c, p.IsActive), c.ToLocal(p.CreatedAtUtc))).ToList(),
            filters, Summary: StatusSummary(c, rows.Count(p => p.IsActive), rows.Count(p => !p.IsActive)));
    }

    public static TabularReport ProductUnits(
        DocumentContext c, IReadOnlyList<UnitConversionResponse> rows, ExportFormat format, IReadOnlyList<ReportFilter> filters)
    {
        var title = c.L("مرجع الوحدات ومعاملات التحويل", "Unit and conversion reference");
        if (format == ExportFormat.Pdf)
            return new TabularReport(c.Language, "catalog-product-units", title,
                [
                    new(c.L("كود الصنف", "SKU"), ColumnKind.Code, 1.2f),
                    new(c.L("الصنف", "Product"), ColumnKind.Text, 3f),
                    new(c.L("الوحدة", "Unit"), ColumnKind.Text, 1.3f),
                    new(c.L("معامل التحويل", "Factor"), ColumnKind.Quantity, 1f),
                    new(c.L("الوحدة الأساسية", "Base unit"), ColumnKind.Text, 1.2f),
                    new(c.L("أساسية", "Base"), ColumnKind.Text, 0.6f),
                    new(c.L("الباركود", "Barcodes"), ColumnKind.Count, 0.7f),
                    new(c.L("الحالة", "Status"), ColumnKind.Text, 0.75f),
                ],
                rows.Select(u => Row(u.Sku, c.Name(u.ProductNameAr, u.ProductNameEn), $"{u.UnitCode} · {c.Name(u.UnitNameAr, u.UnitNameEn)}",
                    u.ConversionFactor, u.BaseUnitCode, u.IsBase, u.BarcodeCount, CatalogLabels.Status(c, u.IsActive))).ToList(),
                filters, Landscape: true);

        return new TabularReport(c.Language, "catalog-product-units", title,
            [
                new(c.L("كود الصنف", "SKU"), ColumnKind.Code),
                new(c.L("الاسم العربي", "Arabic name")),
                new(c.L("الاسم الإنجليزي", "English name")),
                new(c.L("حالة الصنف", "Product status")),
                new(c.L("كود الوحدة", "Unit code"), ColumnKind.Code),
                new(c.L("الوحدة", "Unit")),
                new(c.L("معامل التحويل", "Conversion factor"), ColumnKind.Quantity),
                new(c.L("كود الوحدة الأساسية", "Base unit code"), ColumnKind.Code),
                new(c.L("أساسية", "Is base unit")),
                new(c.L("الباركود النشط", "Active barcodes"), ColumnKind.Count),
                new(c.L("حالة الوحدة", "Unit status")),
            ],
            rows.Select(u => Row(u.Sku, u.ProductNameAr, u.ProductNameEn, CatalogLabels.Status(c, u.ProductIsActive), u.UnitCode,
                c.Name(u.UnitNameAr, u.UnitNameEn), u.ConversionFactor, u.BaseUnitCode, u.IsBase, u.BarcodeCount,
                CatalogLabels.Status(c, u.IsActive))).ToList(),
            filters);
    }

    public static TabularReport Barcodes(
        DocumentContext c, IReadOnlyList<BarcodeReportResponse> rows, ExportFormat format, IReadOnlyList<ReportFilter> filters)
    {
        var title = c.L("باركود الأصناف", "Product barcodes");
        if (format == ExportFormat.Pdf)
            return new TabularReport(c.Language, "catalog-barcodes", title,
                [
                    new(c.L("الباركود", "Barcode"), ColumnKind.Code, 1.6f),
                    new(c.L("النوع", "Type"), ColumnKind.Text, 0.8f),
                    new(c.L("كود الصنف", "SKU"), ColumnKind.Code, 1.2f),
                    new(c.L("الصنف", "Product"), ColumnKind.Text, 3f),
                    new(c.L("الوحدة", "Unit"), ColumnKind.Text, 1f),
                    new(c.L("معامل التحويل", "Factor"), ColumnKind.Quantity, 0.9f),
                    new(c.L("الحالة", "Status"), ColumnKind.Text, 0.75f),
                ],
                rows.Select(b => Row(b.Barcode, CatalogLabels.Barcode(c, b.BarcodeType), b.Sku, c.Name(b.ProductNameAr, b.ProductNameEn),
                    b.UnitCode, b.ConversionFactor, CatalogLabels.Status(c, b.IsActive))).ToList(),
                filters, Landscape: true);

        return new TabularReport(c.Language, "catalog-barcodes", title,
            [
                new(c.L("الباركود", "Barcode"), ColumnKind.Code),
                new(c.L("نوع الباركود", "Barcode type")),
                new(c.L("كود الصنف", "SKU"), ColumnKind.Code),
                new(c.L("الاسم العربي", "Arabic name")),
                new(c.L("الاسم الإنجليزي", "English name")),
                new(c.L("كود الوحدة", "Unit code"), ColumnKind.Code),
                new(c.L("الوحدة", "Unit")),
                new(c.L("معامل التحويل", "Conversion factor"), ColumnKind.Quantity),
                new(c.L("الحالة", "Status")),
            ],
            rows.Select(b => Row(b.Barcode, CatalogLabels.Barcode(c, b.BarcodeType), b.Sku, b.ProductNameAr, b.ProductNameEn, b.UnitCode,
                c.Name(b.UnitNameAr, b.UnitNameEn), b.ConversionFactor, CatalogLabels.Status(c, b.IsActive))).ToList(),
            filters);
    }

    public static TabularReport Categories(
        DocumentContext c, IReadOnlyList<CategoryProductCountResponse> rows, ExportFormat format, IReadOnlyList<ReportFilter> filters)
    {
        var none = c.L("(بدون تصنيف)", "(No category)");
        // On paper the tree shows as a dash per level (leading spaces don't survive layout); spreadsheets get the level
        // as a number to sort and filter on.
        string Name(CategoryProductCountResponse r) => r.Id is null
            ? none
            : (format == ExportFormat.Pdf ? string.Concat(Enumerable.Repeat("– ", r.Level)) : "") + c.Name(r.NameAr!, r.NameEn);
        var totals = (Active: rows.Sum(r => r.ActiveProducts), Inactive: rows.Sum(r => r.InactiveProducts));
        var title = c.L("قائمة التصنيفات وعدد الأصناف", "Category list and product counts");
        if (format == ExportFormat.Pdf)
            return new TabularReport(c.Language, "catalog-categories", title,
                [
                    new(c.L("الكود", "Code"), ColumnKind.Code, 1.2f),
                    new(c.L("التصنيف", "Category"), ColumnKind.Text, 3f),
                    new(c.L("التصنيف الأب", "Parent"), ColumnKind.Code, 1.2f),
                    new(c.L("الحالة", "Status"), ColumnKind.Text, 0.8f),
                    new(c.L("أصناف نشطة", "Active products"), ColumnKind.Count, 0.9f),
                    new(c.L("أصناف غير نشطة", "Inactive products"), ColumnKind.Count, 0.9f),
                    new(c.L("الإجمالي", "Total"), ColumnKind.Count, 0.8f),
                ],
                rows.Select(r => Row(r.Code, Name(r), r.ParentCode, r.Id is null ? null : CatalogLabels.Status(c, r.IsActive),
                    r.ActiveProducts, r.InactiveProducts, r.ActiveProducts + r.InactiveProducts)).ToList(),
                filters, Totals: Row(c.L("الإجمالي", "Total"), null, null, null, totals.Active, totals.Inactive, totals.Active + totals.Inactive));

        return new TabularReport(c.Language, "catalog-categories", title,
            [
                new(c.L("الكود", "Code"), ColumnKind.Code),
                new(c.L("الاسم العربي", "Arabic name")),
                new(c.L("الاسم الإنجليزي", "English name")),
                new(c.L("كود التصنيف الأب", "Parent code"), ColumnKind.Code),
                new(c.L("المستوى", "Level"), ColumnKind.Count),
                new(c.L("الحالة", "Status")),
                new(c.L("أصناف نشطة", "Active products"), ColumnKind.Count),
                new(c.L("أصناف غير نشطة", "Inactive products"), ColumnKind.Count),
                new(c.L("الإجمالي", "Total"), ColumnKind.Count),
            ],
            rows.Select(r => Row(r.Code, r.Id is null ? none : r.NameAr, r.NameEn, r.ParentCode, r.Id is null ? null : r.Level,
                r.Id is null ? null : CatalogLabels.Status(c, r.IsActive), r.ActiveProducts, r.InactiveProducts,
                r.ActiveProducts + r.InactiveProducts)).ToList(),
            filters, Totals: Row(c.L("الإجمالي", "Total"), null, null, null, null, null, totals.Active, totals.Inactive, totals.Active + totals.Inactive));
    }

    public static TabularReport Units(DocumentContext c, IReadOnlyList<UnitUsageResponse> rows, IReadOnlyList<ReportFilter> filters) =>
        new(c.Language, "catalog-units", c.L("وحدات القياس", "Units of measure"),
            [
                new(c.L("الكود", "Code"), ColumnKind.Code, 1f),
                new(c.L("الاسم العربي", "Arabic name"), ColumnKind.Text, 2f),
                new(c.L("الاسم الإنجليزي", "English name"), ColumnKind.Text, 2f),
                new(c.L("تقبل الكسور", "Allows fractions"), ColumnKind.Text, 0.9f),
                new(c.L("الحالة", "Status"), ColumnKind.Text, 0.8f),
                new(c.L("أصناف بها كوحدة أساسية", "Products (base unit)"), ColumnKind.Count, 1.1f),
                new(c.L("وحدات أصناف", "Product units"), ColumnKind.Count, 0.9f),
            ],
            rows.Select(u => Row(u.Code, u.NameAr, u.NameEn, u.AllowsFraction, CatalogLabels.Status(c, u.IsActive),
                u.BaseUnitProducts, u.ProductUnits)).ToList(),
            filters);

    public static TabularReport DataQuality(
        DocumentContext c, IReadOnlyList<DataQualityIssueResponse> rows, IReadOnlyList<ReportFilter> filters) =>
        new(c.Language, "catalog-data-quality", c.L("تقرير جودة بيانات الأصناف", "Catalog data-quality report"),
            [
                new(c.L("كود الصنف", "SKU"), ColumnKind.Code, 1.2f),
                new(c.L("الاسم العربي", "Arabic name"), ColumnKind.Text, 2.6f),
                new(c.L("الاسم الإنجليزي", "English name"), ColumnKind.Text, 2f),
                new(c.L("الملاحظة", "Issue"), ColumnKind.Text, 1.9f),
                new(c.L("التفاصيل", "Detail"), ColumnKind.Code, 1.3f),
                new(c.L("حالة الصنف", "Product status"), ColumnKind.Text, 0.8f),
            ],
            rows.Select(i => Row(i.Sku, i.NameAr, i.NameEn, CatalogLabels.Issue(c, i.Issue), i.Detail,
                CatalogLabels.Status(c, i.ProductIsActive))).ToList(),
            filters, Landscape: true,
            Summary: Enum.GetValues<CatalogDataIssue>()
                .Select(issue => (issue, count: rows.Count(i => i.Issue == issue)))
                .Where(x => x.count > 0)
                .Select(x => new ReportFilter(CatalogLabels.Issue(c, x.issue), x.count.ToString(System.Globalization.CultureInfo.InvariantCulture)))
                .ToList());

    private static string? CategoryName(DocumentContext c, ProductMasterResponse p) =>
        p.CategoryNameAr is null ? null : c.Name(p.CategoryNameAr, p.CategoryNameEn);

    private static List<ReportFilter> StatusSummary(DocumentContext c, int active, int inactive) =>
    [
        new(c.L("نشط", "Active"), active.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        new(c.L("غير نشط", "Inactive"), inactive.ToString(System.Globalization.CultureInfo.InvariantCulture)),
    ];

    private static object?[] Row(params object?[] values) => values;
}
