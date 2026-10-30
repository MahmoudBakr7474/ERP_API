using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Organization.Application.Contracts.Imports;

namespace WarehouseERP.Organization.Presentation.Imports;

// Import problems in Arabic for the downloadable error report (the API's own messages are English; the web app
// translates the codes itself). An unknown code keeps the English message.
internal static class LocationImportMessages
{
    private static readonly Dictionary<string, string> Arabic = new(StringComparer.Ordinal)
    {
        ["Organization.Import.Required"] = "قيمة مطلوبة.",
        ["Organization.Import.TooLong"] = "النص أطول من المسموح لهذا العمود.",
        ["Organization.Import.InvalidFormat"] = "مسموح فقط بالحروف الإنجليزية والأرقام و . _ - في كود الموقع.",
        ["Organization.Import.DuplicateInFile"] = "نفس الموقع مكرر في الملف.",
        ["Organization.Import.NotPermitted"] = "لا تملك الصلاحية اللازمة لهذا الصف.",
        ["Organization.Import.MissingColumn"] = "عمود مطلوب غير موجود في الورقة؛ استخدم صف العناوين في القالب.",
        ["Organization.Import.NoRows"] = "لا توجد صفوف للاستيراد في الملف.",
        ["Organization.Import.TransitWarehouse"] = "مواقع مخزن الترانزيت تفتحها وتغلقها التحويلات المخزنية فقط.",
        ["Organization.Import.DefaultLocationFixed"] = "الموقع الافتراضي يُنشأ مع المخزن ولا يغيّره الاستيراد.",
        ["Organization.Branch.NotFound"] = "لا يوجد فرع بهذا الكود.",
        ["Organization.Warehouse.NotFound"] = "لا يوجد مخزن بهذا الكود في هذا الفرع.",
        ["Organization.Warehouse.AccessDenied"] = "غير مسموح لك بالوصول إلى هذا المخزن.",
        ["Organization.Warehouse.NotActive"] = "المخزن غير نشط؛ لا تُضاف إليه مواقع جديدة.",
        ["Organization.WarehouseLocation.CodeTaken"] = "للمخزن موقع بهذا الكود بالفعل (وضع الإنشاء يضيف الجديد فقط).",
    };

    public static string For(DocumentContext c, LocationImportIssue issue) =>
        c.IsArabic && Arabic.TryGetValue(issue.Code, out var text) ? text : issue.Message;
}
