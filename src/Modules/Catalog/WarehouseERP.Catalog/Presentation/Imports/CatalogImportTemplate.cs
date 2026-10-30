using ClosedXML.Excel;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Catalog.Application.Contracts.Reports;

namespace WarehouseERP.Catalog.Presentation.Imports;

// The import workbook users fill in: instructions with examples first, then one sheet per kind of row (headers in the
// chosen language, required ones marked *, code columns stored as text so leading zeros survive, drop-down lists for
// choices), and a reference sheet with the active units and categories the rows may name. It carries no example
// rows in the data sheets, so an untouched template imports nothing.
internal static class CatalogImportTemplate
{
    private const int TemplateRows = 5_000;

    public static byte[] Write(DocumentContext c, IReadOnlyList<UnitUsageResponse> units, IReadOnlyList<CategoryProductCountResponse> categories)
    {
        var arabic = c.IsArabic;
        using var workbook = new XLWorkbook();
        workbook.Properties.Title = c.L("قالب استيراد بيانات الأصناف", "Catalog import template");
        workbook.Properties.Author = "WarehouseERP";

        var instructions = workbook.Worksheets.Add(c.L("التعليمات", "Instructions"));
        var lists = BuildLists(workbook, c, units, categories);
        Instructions(instructions, c);

        foreach (var layout in CatalogImportLayout.Sheets)
        {
            var sheet = workbook.Worksheets.Add(layout.Name(arabic));
            sheet.RightToLeft = arabic;
            for (var i = 0; i < layout.Columns.Count; i++)
            {
                var column = layout.Columns[i];
                var header = sheet.Cell(1, i + 1);
                header.Value = column.Header(arabic);
                header.Style.Font.Bold = true;
                header.Style.Font.FontColor = XLColor.White;
                header.Style.Fill.BackgroundColor = XLColor.FromHtml(column.Required ? BrandedPdf.Navy : "#1E4E8C");
                header.CreateComment().AddText(arabic ? column.HelpAr : column.HelpEn);
                var cells = sheet.Range(2, i + 1, TemplateRows + 1, i + 1);
                if (column.Kind is ImportColumnKind.Code or ImportColumnKind.Text)
                    cells.Style.NumberFormat.Format = "@";
                if (column.Choices is { } choices && lists.TryGetValue(column.Key, out var range))
                {
                    var validation = cells.CreateDataValidation();
                    validation.List(range, true);
                    validation.IgnoreBlanks = true;
                    validation.ErrorStyle = XLErrorStyle.Stop;
                    validation.ErrorTitle = column.Header(arabic);
                    validation.ErrorMessage = string.Join(" / ", choices.Select(x => arabic ? x.LabelAr : x.LabelEn));
                }
                sheet.Column(i + 1).Width = Math.Max(16, column.Header(arabic).Length + 6);
            }
            sheet.SheetView.FreezeRows(1);
        }

        instructions.SetTabActive();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // The reference sheet: choice labels (the drop-downs' source) and the active units and categories.
    private static Dictionary<string, IXLRange> BuildLists(
        XLWorkbook workbook, DocumentContext c, IReadOnlyList<UnitUsageResponse> units, IReadOnlyList<CategoryProductCountResponse> categories)
    {
        var arabic = c.IsArabic;
        var sheet = workbook.Worksheets.Add(c.L("القوائم", "Lists"));
        sheet.RightToLeft = arabic;
        var ranges = new Dictionary<string, IXLRange>(StringComparer.Ordinal);
        var col = 1;
        foreach (var column in CatalogImportLayout.Sheets.SelectMany(s => s.Columns).Where(x => x.Choices is not null).DistinctBy(x => x.Key))
        {
            Head(sheet.Cell(1, col), column.Header(arabic).TrimEnd('*', ' '));
            var choices = column.Choices!;
            for (var r = 0; r < choices.Count; r++)
                sheet.Cell(r + 2, col).SetValue(arabic ? choices[r].LabelAr : choices[r].LabelEn);
            ranges[column.Key] = sheet.Range(2, col, choices.Count + 1, col);
            sheet.Column(col).Width = 18;
            col++;
        }

        col++;
        Head(sheet.Cell(1, col), c.L("كود الوحدة", "Unit code"));
        Head(sheet.Cell(1, col + 1), c.L("الوحدة", "Unit"));
        Head(sheet.Cell(1, col + 2), c.L("تقبل الكسور", "Allows fractions"));
        var row = 2;
        foreach (var unit in units.Where(u => u.IsActive))
        {
            sheet.Cell(row, col).SetValue(unit.Code);
            sheet.Cell(row, col + 1).SetValue(c.Name(unit.NameAr, unit.NameEn));
            sheet.Cell(row, col + 2).SetValue(unit.AllowsFraction ? c.L("نعم", "Yes") : c.L("لا", "No"));
            row++;
        }
        sheet.Column(col).Width = 14;
        sheet.Column(col + 1).Width = 24;
        sheet.Column(col + 2).Width = 14;

        col += 4;
        Head(sheet.Cell(1, col), c.L("كود التصنيف", "Category code"));
        Head(sheet.Cell(1, col + 1), c.L("التصنيف", "Category"));
        row = 2;
        foreach (var category in categories.Where(x => x.Id is not null && x.IsActive))
        {
            sheet.Cell(row, col).SetValue(category.Code);
            sheet.Cell(row, col + 1).SetValue(new string(' ', category.Level * 2) + c.Name(category.NameAr!, category.NameEn));
            row++;
        }
        sheet.Column(col).Width = 16;
        sheet.Column(col + 1).Width = 30;
        sheet.SheetView.FreezeRows(1);
        return ranges;
    }

    private static void Instructions(IXLWorksheet sheet, DocumentContext c)
    {
        var arabic = c.IsArabic;
        sheet.RightToLeft = arabic;
        sheet.Column(1).Width = 120;
        var row = 1;
        void Line(string ar, string en, bool bold = false, double size = 11)
        {
            var cell = sheet.Cell(row++, 1);
            cell.SetValue(c.L(ar, en));
            cell.Style.Alignment.WrapText = true;
            cell.Style.Font.Bold = bold;
            cell.Style.Font.FontSize = size;
        }

        Line("قالب استيراد بيانات الأصناف", "Catalog import template", bold: true, size: 15);
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml(BrandedPdf.Navy);
        row++;
        Line("1. املأ الأوراق التي تحتاجها فقط، واترك أسماء الأوراق وصف العناوين كما هي. الأعمدة المعلّمة بـ * مطلوبة؛ تعليق كل عنوان يشرح العمود.",
            "1. Fill in only the sheets you need and keep the sheet names and header rows as they are. Columns marked * are required; each header's note explains it.");
        Line("2. ترتيب الاستيراد: الوحدات ← التصنيفات ← الأصناف ← وحدات الأصناف ← الباركود. يمكن للصف أن يشير إلى كود أُضيف في ورقة سابقة من نفس الملف.",
            "2. Import order: Units, Categories, Products, ProductUnits, Barcodes. A row may refer to a code added in an earlier sheet of the same file.");
        Line("3. عند الرفع اختر الوضع: «إنشاء» يضيف الجديد فقط والكود الموجود خطأ؛ «تحديث» يضيف الجديد ويحدّث الأسماء والتصنيف للموجود؛ «تخطي» يضيف الجديد ويترك الموجود كما هو.",
            "3. Choose the mode when uploading: Create adds new records only (an existing code is an error); Update adds new records and updates names and category of existing ones; Skip adds new records and leaves existing ones as they are.");
        Line("4. لا تتغير بعد الإنشاء: كود الصنف ونوعه وتتبعه ووحدته الأساسية، وأكواد الوحدات والتصنيفات، ومعاملات التحويل، وقيمة الباركود ونوعه. الخانة الاختيارية الفارغة تُبقي القيمة الحالية في وضع التحديث.",
            "4. Fixed once created: a product's SKU, type, tracking and base unit, unit and category codes, conversion factors, barcode values and types. In Update mode an empty optional cell keeps the current value.");
        Line("5. معامل التحويل = عدد الوحدات الأساسية في الوحدة: إذا كانت الكرتونة 12 قطعة فالمعامل 12. عدد صحيح إذا كانت الوحدة الأساسية لا تقبل الكسور، وحتى 8 منازل عشرية بنقطة.",
            "5. Conversion factor = base units in one unit: a box of 12 pieces has factor 12. Whole when the base unit allows no fractions; up to 8 decimals with a dot.");
        Line("6. الباركود: EAN-13 وGTIN يلزمهما رقم تحقق GS1 صحيح؛ داخلي وآخر أكواد الشركة بدون مسافات. لا تُعاد قيمة باركود أبدًا حتى بعد إيقافه.",
            "6. Barcodes: EAN-13 and GTIN need a valid GS1 check digit; Internal and Other are the company's own codes without spaces. A barcode value is never reused, even after it is retired.");
        Line("7. لا تُستورد كميات المخزون هنا: الأرصدة الافتتاحية تُسجَّل بمستندات المخزون.",
            "7. Stock quantities are not imported here: opening balances are recorded with Inventory documents.");
        Line("8. لا يُحفظ شيء قبل مراجعة المعاينة والتأكيد. إذا كان في أي صف خطأ لا يُستورد أي شيء.",
            "8. Nothing is saved until you review the preview and confirm. If any row has an error, nothing is imported.");
        row++;
        Line("مثال", "Example", bold: true, size: 12);
        Line("الوحدات: PCS | قطعة | Piece | لا  —  BOX | كرتونة | Box | لا  —  KG | كيلوجرام | Kilogram | نعم",
            "Units: PCS | قطعة | Piece | No  —  BOX | كرتونة | Box | No  —  KG | كيلوجرام | Kilogram | Yes");
        Line("التصنيفات: FOOD | مواد غذائية | Food |   —  OIL | زيوت | Oils | FOOD",
            "Categories: FOOD | مواد غذائية | Food |   —  OIL | زيوت | Oils | FOOD");
        Line("الأصناف: OIL-5L | زيت عباد الشمس 5 لتر | Sunflower oil 5L | PCS | OIL | صنف مخزني | بدون تتبع",
            "Products: OIL-5L | زيت عباد الشمس 5 لتر | Sunflower oil 5L | PCS | OIL | Stock item | None");
        Line("وحدات الأصناف: OIL-5L | BOX | 4   (الكرتونة = 4 عبوات)", "ProductUnits: OIL-5L | BOX | 4   (one box holds 4 bottles)");
        Line("الباركود: OIL-5L | PCS | 6221234567891 | EAN-13  —  OIL-5L | BOX | BOX-OIL-5L | داخلي",
            "Barcodes: OIL-5L | PCS | 6221234567891 | EAN-13  —  OIL-5L | BOX | BOX-OIL-5L | Internal");
    }

    private static void Head(IXLCell cell, string text)
    {
        cell.SetValue(text);
        cell.Style.Font.Bold = true;
        cell.Style.Font.FontColor = XLColor.White;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(BrandedPdf.Navy);
    }
}
