using ClosedXML.Excel;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Presentation.Documents;

namespace WarehouseERP.Organization.Presentation.Imports;

// The workbook users fill in: instructions with an example first, then the Locations sheet (headers in the chosen
// language, required ones marked *, code columns stored as text), and a Lists sheet with the physical warehouses the
// user may import into, by branch. It carries no example rows in the data sheet, so an untouched template imports nothing.
internal static class LocationImportTemplate
{
    public static byte[] Write(DocumentContext c, IReadOnlyList<WarehouseSummaryResponse> warehouses)
    {
        var arabic = c.IsArabic;
        using var workbook = new XLWorkbook();
        workbook.Properties.Title = c.L("قالب استيراد مواقع التخزين", "Storage location import template");
        workbook.Properties.Author = "WarehouseERP";

        var instructions = workbook.Worksheets.Add(c.L("التعليمات", "Instructions"));
        Instructions(instructions, c);
        ImportTemplateSheet.Add(workbook, arabic ? LocationImportLayout.SheetNameAr : LocationImportLayout.SheetNameEn,
            LocationImportLayout.Columns, arabic, new Dictionary<string, IXLRange>());
        Lists(workbook, c, warehouses);

        instructions.SetTabActive();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void Lists(XLWorkbook workbook, DocumentContext c, IReadOnlyList<WarehouseSummaryResponse> warehouses)
    {
        var sheet = workbook.Worksheets.Add(c.L("القوائم", "Lists"));
        sheet.RightToLeft = c.IsArabic;
        string[] headers =
        [
            c.L("كود الفرع", "Branch code"), c.L("الفرع", "Branch"), c.L("كود المخزن", "Warehouse code"), c.L("المخزن", "Warehouse"),
            c.L("الحالة", "Status"), c.L("المواقع", "Locations"),
        ];
        for (var i = 0; i < headers.Length; i++)
            ImportTemplateSheet.Head(sheet.Cell(1, i + 1), headers[i]);
        var row = 2;
        foreach (var w in warehouses)
        {
            sheet.Cell(row, 1).SetValue(w.BranchCode);
            sheet.Cell(row, 2).SetValue(c.Name(w.BranchNameAr, w.BranchNameEn));
            sheet.Cell(row, 3).SetValue(w.Code);
            sheet.Cell(row, 4).SetValue(c.Name(w.NameAr, w.NameEn));
            sheet.Cell(row, 5).SetValue(OrganizationLabels.Status(c, w.IsActive));
            sheet.Cell(row, 6).SetValue(w.LocationCount);
            row++;
        }
        sheet.Column(1).Width = 14;
        sheet.Column(2).Width = 28;
        sheet.Column(3).Width = 16;
        sheet.Column(4).Width = 30;
        sheet.Column(5).Width = 12;
        sheet.Column(6).Width = 10;
        sheet.SheetView.FreezeRows(1);
    }

    private static void Instructions(IXLWorksheet sheet, DocumentContext c)
    {
        sheet.RightToLeft = c.IsArabic;
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

        Line("قالب استيراد مواقع التخزين", "Storage location import template", bold: true, size: 15);
        sheet.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml(BrandedPdf.Navy);
        row++;
        Line("1. املأ ورقة «المواقع» واترك اسمها وصف العناوين كما هما. الأعمدة المعلّمة بـ * مطلوبة؛ تعليق كل عنوان يشرح العمود. يمكن أيضًا رفع ملف CSV بنفس العناوين.",
            "1. Fill in the Locations sheet and keep its name and header row. Columns marked * are required; each header's note explains it. A CSV file with the same headers works too.");
        Line("2. يُحدَّد المخزن بكود الفرع وكود المخزن معًا (أكواد المخازن تتكرر بين الفروع). ورقة «القوائم» فيها المخازن الفعلية المتاحة لك.",
            "2. A warehouse is named by its branch code and its own code together (warehouse codes repeat across branches). The Lists sheet has the physical warehouses you may use.");
        Line("3. عند الرفع اختر الوضع: «إنشاء» يضيف المواقع الجديدة فقط والموقع الموجود خطأ؛ «تحديث» يضيف الجديد ويغيّر أسماء الموجود؛ «تخطي» يضيف الجديد ويترك الموجود كما هو.",
            "3. Choose the mode when uploading: Create adds new locations only (an existing one is an error); Update adds new ones and renames existing ones; Skip adds new ones and leaves existing ones as they are.");
        Line("4. لا يغيّر الاستيراد أبدًا كود موقع ولا مخزنه ولا حالته (التفعيل والإيقاف من شاشة المخزن، حيث يُتحقق من المخزون)، ولا الموقع الافتراضي، ولا مواقع مخازن الترانزيت.",
            "4. An import never changes a location's code, warehouse or status (activate and deactivate on the warehouse screen, which checks stock), never the default location, and never a transit warehouse's locations.");
        Line("5. لا يُحفظ شيء قبل مراجعة المعاينة والتأكيد. إذا كان في أي صف خطأ لا يُستورد أي شيء.",
            "5. Nothing is saved until you review the preview and confirm. If any row has an error, nothing is imported.");
        row++;
        Line("مثال", "Example", bold: true, size: 12);
        Line("المواقع: CAI | WH-01 | A.01-01 | ممر أ رف 1 مستوى 1 | Aisle A rack 1 level 1", "Locations: CAI | WH-01 | A.01-01 | ممر أ رف 1 مستوى 1 | Aisle A rack 1 level 1");
        Line("          CAI | WH-01 | A.01-02 | ممر أ رف 1 مستوى 2 |", "          CAI | WH-01 | A.01-02 | ممر أ رف 1 مستوى 2 |");
    }
}
