using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Catalog.Application.Contracts.Reports;

namespace WarehouseERP.Catalog.Presentation.Documents;

// The product specification sheet: one product on A4 portrait with its master data, every unit and its conversion,
// and every barcode. The SKU is the document number.
internal static class ProductSheetDocument
{
    public static byte[] Write(DocumentContext c, ProductSheetResponse sheet)
    {
        var p = sheet.Product;
        var info = new PdfDocumentInfo(c.L("بطاقة مواصفات صنف", "Product specification sheet"), DocumentNumber: p.Sku);
        return BrandedPdf.Generate(c, info, body => body.Column(column =>
        {
            column.Spacing(12);
            column.Item().Element(x => Heading(x, c, p));
            column.Item().Element(x => Details(x, c, sheet));
            column.Item().Element(x => Section(x, c.L("الوحدات ومعاملات التحويل", "Units and conversion factors")));
            column.Item().Element(x => TabularPdfWriter.Table(x, Units(c, sheet), c));
            column.Item().Element(x => Section(x, c.L("الباركود", "Barcodes")));
            column.Item().Element(x => TabularPdfWriter.Table(x, Barcodes(c, sheet), c));
        }));
    }

    private static void Heading(IContainer container, DocumentContext c, ProductMasterResponse p) =>
        container.Column(column =>
        {
            column.Item().Value(c, p.NameAr, x => x.FontSize(14).Bold());
            if (p.NameEn is { } nameEn)
                column.Item().Value(c, nameEn, x => x.FontSize(10.5f).FontColor(BrandedPdf.Muted));
        });

    private static void Details(IContainer container, DocumentContext c, ProductSheetResponse sheet)
    {
        var p = sheet.Product;
        var fields = new List<(string Label, string Value)>
        {
            (c.L("كود الصنف", "SKU"), p.Sku),
            (c.L("الحالة", "Status"), CatalogLabels.Status(c, p.IsActive)),
            (c.L("التصنيف", "Category"), p.CategoryCode is null ? "—" : $"{p.CategoryCode} · {c.Name(p.CategoryNameAr!, p.CategoryNameEn)}"),
            (c.L("نوع الصنف", "Product type"), CatalogLabels.Type(c, p.Type)),
            (c.L("الوحدة الأساسية", "Base unit"), $"{p.BaseUnitCode} · {c.Name(p.BaseUnitNameAr, p.BaseUnitNameEn)}"),
            (c.L("التتبع", "Tracking"), CatalogLabels.Tracking(c, p.TrackingMode)),
            (c.L("الوحدات النشطة", "Active units"), p.UnitCount.ToString(CultureInfo.InvariantCulture)),
            (c.L("الباركود النشط", "Active barcodes"), p.BarcodeCount.ToString(CultureInfo.InvariantCulture)),
            (c.L("تاريخ الإنشاء", "Created"), Time(c, p.CreatedAtUtc)),
            (c.L("آخر تعديل", "Last changed"), sheet.UpdatedAtUtc is { } updated ? Time(c, updated) : "—"),
        };
        container.Border(0.6f).BorderColor(BrandedPdf.Border).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(2f);
                columns.RelativeColumn(1.1f);
                columns.RelativeColumn(2f);
            });
            foreach (var (label, value) in fields)
            {
                table.Cell().Background(BrandedPdf.Shade).BorderBottom(0.5f).BorderColor(BrandedPdf.Border).Padding(5)
                    .Text(t => t.AlignStart(c).Span(label).FontSize(8.5f).FontColor(BrandedPdf.Muted));
                table.Cell().BorderBottom(0.5f).BorderColor(BrandedPdf.Border).Padding(5)
                    .Value(c, value, x => x.FontSize(9).SemiBold());
            }
        });
    }

    private static void Section(IContainer container, string title) =>
        container.PaddingTop(4).BorderBottom(1).BorderColor(BrandedPdf.Primary).PaddingBottom(3)
            .Text(title).FontSize(11).SemiBold().FontColor(BrandedPdf.Navy);

    private static TabularReport Units(DocumentContext c, ProductSheetResponse sheet) =>
        new(c.Language, "product-units", "",
            [
                new(c.L("كود الوحدة", "Unit code"), ColumnKind.Code, 1f),
                new(c.L("الوحدة", "Unit"), ColumnKind.Text, 1.6f),
                new(c.L("معامل التحويل", "Conversion factor"), ColumnKind.Quantity, 1.1f),
                new(c.L("التحويل", "Conversion"), ColumnKind.Code, 1.8f),
                new(c.L("أساسية", "Base"), ColumnKind.Text, 0.6f),
                new(c.L("الحالة", "Status"), ColumnKind.Text, 0.8f),
            ],
            sheet.Units.Select(u => (IReadOnlyList<object?>)new object?[]
            {
                u.UnitCode, c.Name(u.UnitNameAr, u.UnitNameEn), u.ConversionFactor,
                string.Create(CultureInfo.InvariantCulture, $"1 {u.UnitCode} = {u.ConversionFactor:0.########} {sheet.Product.BaseUnitCode}"),
                u.IsBase, CatalogLabels.Status(c, u.IsActive),
            }).ToList(),
            []);

    private static TabularReport Barcodes(DocumentContext c, ProductSheetResponse sheet) =>
        new(c.Language, "product-barcodes", "",
            [
                new(c.L("الباركود", "Barcode"), ColumnKind.Code, 1.8f),
                new(c.L("النوع", "Type"), ColumnKind.Text, 0.9f),
                new(c.L("الوحدة", "Unit"), ColumnKind.Code, 1f),
                new(c.L("الحالة", "Status"), ColumnKind.Text, 0.8f),
            ],
            sheet.Units.SelectMany(u => u.Barcodes.Select(b => (IReadOnlyList<object?>)new object?[]
            {
                b.Barcode, CatalogLabels.Barcode(c, b.BarcodeType), u.UnitCode, CatalogLabels.Status(c, b.IsActive),
            })).ToList(),
            []);

    private static string Time(DocumentContext c, DateTime utc) =>
        c.ToLocal(utc).ToString(ReportValues.DateTimeFormat, CultureInfo.InvariantCulture);
}
