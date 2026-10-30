using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Presentation.Imports;

// The import file's layout, in one place for the reader, the template and the error report. Headers and choices are
// matched by the shared ImportColumns rules (either language, the key, case and spaces ignored).
internal static class CatalogImportLayout
{
    private static readonly ImportChoice[] YesNo = [new("true", "Yes", "نعم"), new("false", "No", "لا")];

    private static readonly ImportChoice[] ProductTypes =
    [
        new(nameof(ProductType.StockItem), "Stock item", "صنف مخزني"),
        new(nameof(ProductType.NonStockItem), "Non-stock item", "صنف غير مخزني"),
        new(nameof(ProductType.Service), "Service", "خدمة"),
    ];

    private static readonly ImportChoice[] TrackingModes =
    [
        new(nameof(TrackingMode.None), "None", "بدون تتبع"),
        new(nameof(TrackingMode.Batch), "Batch", "تشغيلة"),
        new(nameof(TrackingMode.Serial), "Serial", "سيريال"),
    ];

    private static readonly ImportChoice[] BarcodeTypes =
    [
        new(nameof(BarcodeType.Internal), "Internal", "داخلي"),
        new(nameof(BarcodeType.Ean13), "EAN-13", "EAN-13"),
        new(nameof(BarcodeType.Gtin), "GTIN", "GTIN"),
        new(nameof(BarcodeType.Other), "Other", "آخر"),
    ];

    private static ImportColumn Sku(string helpEn, string helpAr) =>
        new("sku", "SKU", "كود الصنف", true, ImportColumnKind.Code, helpEn, helpAr);

    private static ImportColumn NameAr(int max) => new("nameAr", "Arabic name", "الاسم العربي", true, ImportColumnKind.Text,
        $"Up to {max} characters.", $"حتى {max} حرفًا.");

    private static ImportColumn NameEn(int max) => new("nameEn", "English name", "الاسم الإنجليزي", false, ImportColumnKind.Text,
        $"Optional, up to {max} characters. Left empty in Update mode, the current name stays.",
        $"اختياري، حتى {max} حرفًا. إذا تُرك فارغًا في وضع التحديث يبقى الاسم الحالي.");

    public static readonly IReadOnlyList<ImportSheetLayout> Sheets =
    [
        new(CatalogImportSheet.Units, "Units", "الوحدات",
        [
            new("code", "Code", "الكود", true, ImportColumnKind.Code,
                "Up to 20 English letters, digits, - or _. Fixed once created.", "حتى 20 حرفًا إنجليزيًا أو رقمًا أو - أو _. لا يتغير بعد الإنشاء."),
            NameAr(100), NameEn(100),
            new("allowsFraction", "Allows fractions", "تقبل الكسور", true, ImportColumnKind.Choice,
                "Yes for units measured in parts (kg, litre); No for counted units (piece, box).",
                "نعم للوحدات التي تُقاس بأجزاء (كجم، لتر)، ولا للوحدات المعدودة (قطعة، كرتونة).", YesNo),
        ]),
        new(CatalogImportSheet.Categories, "Categories", "التصنيفات",
        [
            new("code", "Code", "الكود", true, ImportColumnKind.Code,
                "Up to 30 English letters, digits, - or _. Fixed once created.", "حتى 30 حرفًا إنجليزيًا أو رقمًا أو - أو _. لا يتغير بعد الإنشاء."),
            NameAr(150), NameEn(150),
            new("parentCode", "Parent code", "كود التصنيف الأب", false, ImportColumnKind.Code,
                "Empty for a top-level category, else an existing code or one from this sheet.",
                "فارغ للتصنيف الرئيسي، أو كود تصنيف موجود أو من هذه الورقة."),
        ]),
        new(CatalogImportSheet.Products, "Products", "الأصناف",
        [
            Sku("Up to 80 English letters, digits and . _ / -. Fixed once created.", "حتى 80 حرفًا إنجليزيًا أو رقمًا و . _ / -. لا يتغير بعد الإنشاء."),
            NameAr(250), NameEn(250),
            new("baseUnitCode", "Base unit code", "كود الوحدة الأساسية", true, ImportColumnKind.Code,
                "The unit stock is counted in: an active unit code, or one from the Units sheet. Fixed once created.",
                "الوحدة التي يُعد بها المخزون: كود وحدة نشطة أو من ورقة الوحدات. لا تتغير بعد الإنشاء."),
            new("categoryCode", "Category code", "كود التصنيف", false, ImportColumnKind.Code,
                "Optional: an active category code, or one from the Categories sheet.", "اختياري: كود تصنيف نشط أو من ورقة التصنيفات."),
            new("type", "Product type", "نوع الصنف", true, ImportColumnKind.Choice, "Fixed once created.", "لا يتغير بعد الإنشاء.", ProductTypes),
            new("trackingMode", "Tracking", "التتبع", false, ImportColumnKind.Choice,
                "Empty means None. Only stock items can be tracked by batch or serial. Fixed once created.",
                "الفارغ يعني بدون تتبع. التتبع بالتشغيلة أو السيريال للأصناف المخزنية فقط. لا يتغير بعد الإنشاء.", TrackingModes),
        ]),
        new(CatalogImportSheet.ProductUnits, "ProductUnits", "وحدات الأصناف",
        [
            Sku("The product: an existing SKU or one from the Products sheet.", "الصنف: كود صنف موجود أو من ورقة الأصناف."),
            new("unitCode", "Unit code", "كود الوحدة", true, ImportColumnKind.Code,
                "Another unit the product is bought or sold in (the base unit is added with the product).",
                "وحدة أخرى يُشترى أو يُباع بها الصنف (الوحدة الأساسية تُضاف مع الصنف)."),
            new("conversionFactor", "Conversion factor", "معامل التحويل", true, ImportColumnKind.Number,
                "How many base units one of this unit holds: 1 box = 12 pieces gives 12. Up to 8 decimals with a dot; whole when the base unit has no fractions. Fixed once created.",
                "كم وحدة أساسية تحتوي هذه الوحدة: الكرتونة = 12 قطعة تعني 12. حتى 8 منازل عشرية بنقطة، وعدد صحيح إذا كانت الوحدة الأساسية لا تقبل الكسور. لا يتغير بعد الإنشاء."),
        ]),
        new(CatalogImportSheet.Barcodes, "Barcodes", "الباركود",
        [
            Sku("The product: an existing SKU or one from the Products sheet.", "الصنف: كود صنف موجود أو من ورقة الأصناف."),
            new("unitCode", "Unit code", "كود الوحدة", true, ImportColumnKind.Code,
                "The unit the barcode is printed on: the base unit or one of the product's units.",
                "الوحدة المطبوع عليها الباركود: الوحدة الأساسية أو إحدى وحدات الصنف."),
            new("barcode", "Barcode", "الباركود", true, ImportColumnKind.Code,
                "Keep it as text so leading zeros stay. A value is never reused, even after it is retired.",
                "اكتبه كنص حتى تبقى الأصفار في البداية. لا تُعاد قيمة الباركود أبدًا حتى بعد إيقافه."),
            new("barcodeType", "Barcode type", "نوع الباركود", true, ImportColumnKind.Choice,
                "EAN-13 (13 digits) and GTIN (8, 12, 13 or 14 digits) need a valid GS1 check digit; Internal and Other are your own codes.",
                "EAN-13 (13 رقمًا) وGTIN (8 أو 12 أو 13 أو 14 رقمًا) يلزمهما رقم تحقق GS1 صحيح؛ داخلي وآخر أكواد خاصة بالشركة.", BarcodeTypes),
        ]),
    ];

    public static ImportSheetLayout For(CatalogImportSheet sheet) => Sheets.Single(s => s.Sheet == sheet);

    /// <summary>The layout a workbook sheet is for, by its English or Arabic name; null for any other sheet.</summary>
    public static ImportSheetLayout? Find(string sheetName)
    {
        var name = ImportColumns.Normalize(sheetName);
        return Sheets.FirstOrDefault(s => ImportColumns.Normalize(s.NameEn) == name || ImportColumns.Normalize(s.NameAr) == name);
    }

    /// <summary>A cell as the import rows expect it: a choice's label becomes its API name; anything else is unchanged.</summary>
    public static string? Value(ImportColumn column, string? cell) => ImportColumns.Value(column, cell);
}
