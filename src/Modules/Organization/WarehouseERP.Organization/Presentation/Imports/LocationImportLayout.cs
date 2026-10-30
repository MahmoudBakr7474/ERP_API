using WarehouseERP.BuildingBlocks.Infrastructure.Documents;

namespace WarehouseERP.Organization.Presentation.Imports;

// The location import file's layout, in one place for the reader, the template and the error report. Headers are
// matched by the shared ImportColumns rules (either language, the key, case and spaces ignored).
internal static class LocationImportLayout
{
    public const string SheetNameEn = "Locations";
    public const string SheetNameAr = "المواقع";

    public static readonly IReadOnlyList<ImportColumn> Columns =
    [
        new("branchCode", "Branch code", "كود الفرع", true, ImportColumnKind.Code,
            "The branch of the warehouse, as on the Lists sheet. Warehouse codes repeat across branches, so both are needed.",
            "فرع المخزن كما في ورقة القوائم. أكواد المخازن تتكرر بين الفروع، لذلك يلزم الكودان."),
        new("warehouseCode", "Warehouse code", "كود المخزن", true, ImportColumnKind.Code,
            "An active physical warehouse of that branch, as on the Lists sheet. A location never moves to another warehouse.",
            "مخزن فعلي نشط في ذلك الفرع كما في ورقة القوائم. لا ينتقل الموقع إلى مخزن آخر أبدًا."),
        new("code", "Location code", "كود الموقع", true, ImportColumnKind.Code,
            "Up to 50 English letters, digits and . _ - (for example A.01-02). Unique in the warehouse and fixed once created.",
            "حتى 50 حرفًا إنجليزيًا أو رقمًا و . _ - (مثال A.01-02). فريد داخل المخزن ولا يتغير بعد الإنشاء."),
        new("nameAr", "Arabic name", "الاسم العربي", true, ImportColumnKind.Text, "Up to 150 characters.", "حتى 150 حرفًا."),
        new("nameEn", "English name", "الاسم الإنجليزي", false, ImportColumnKind.Text,
            "Optional, up to 150 characters. Left empty in Update mode, the current name stays.",
            "اختياري، حتى 150 حرفًا. إذا تُرك فارغًا في وضع التحديث يبقى الاسم الحالي."),
    ];

    public static bool IsLocationsSheet(string sheetName)
    {
        var name = ImportColumns.Normalize(sheetName);
        return name == ImportColumns.Normalize(SheetNameEn) || name == ImportColumns.Normalize(SheetNameAr);
    }

    /// <summary>The column's header in the language, without the required marker (error report, messages).</summary>
    public static string Header(string? key, bool arabic) =>
        Columns.FirstOrDefault(c => c.Key == key) is { } column ? arabic ? column.HeaderAr : column.HeaderEn : key ?? "";
}
