using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Catalog.Application.Contracts.Imports;

namespace WarehouseERP.Catalog.Presentation.Imports;

// Import problems in Arabic for the downloadable error report (the API's own messages are English; the web app
// translates the codes itself). An unknown code keeps the English message.
internal static class CatalogImportMessages
{
    private static readonly Dictionary<string, string> Arabic = new(StringComparer.Ordinal)
    {
        ["Catalog.Import.Required"] = "قيمة مطلوبة.",
        ["Catalog.Import.TooLong"] = "النص أطول من المسموح لهذا العمود.",
        ["Catalog.Import.InvalidFormat"] = "مسموح فقط بالحروف الإنجليزية والأرقام و - _ (و . / في كود الصنف).",
        ["Catalog.Import.InvalidValue"] = "قيمة غير معروفة؛ اختر من القائمة.",
        ["Catalog.Import.InvalidNumber"] = "أدخل عددًا موجبًا بنقطة عشرية، حتى 8 منازل عشرية و10 أرقام صحيحة.",
        ["Catalog.Import.DuplicateInFile"] = "نفس الكود مكرر في هذه الورقة.",
        ["Catalog.Import.NotPermitted"] = "لا تملك الصلاحية اللازمة لهذا الصف.",
        ["Catalog.Import.FixedField"] = "لا يمكن للاستيراد تغيير هذه القيمة لسجل موجود؛ فهي ثابتة بعد الإنشاء.",
        ["Catalog.Import.ReferenceInvalid"] = "يشير إلى صف آخر في الملف به أخطاء.",
        ["Catalog.Import.BaseUnitFactor"] = "معامل الوحدة الأساسية دائمًا 1.",
        ["Catalog.Import.BarcodeInvalid"] = "باركود غير صالح لنوعه: EAN-13 وGTIN يلزمهما عدد الأرقام الصحيح ورقم تحقق GS1.",
        ["Catalog.Import.MissingColumn"] = "عمود مطلوب غير موجود في الورقة؛ استخدم صف العناوين في القالب.",
        ["Catalog.Import.NoRows"] = "لا توجد صفوف للاستيراد في الملف.",
        ["Catalog.Import.DuplicateSheet"] = "الورقة مكررة في الملف؛ أبقِ واحدة فقط.",
        ["Catalog.Unit.CodeTaken"] = "كود الوحدة مستخدم بالفعل (وضع الإنشاء يضيف الجديد فقط).",
        ["Catalog.Unit.NotFound"] = "لا توجد وحدة بهذا الكود.",
        ["Catalog.Unit.NotActive"] = "الوحدة غير نشطة.",
        ["Catalog.Category.CodeTaken"] = "كود التصنيف مستخدم بالفعل (وضع الإنشاء يضيف الجديد فقط).",
        ["Catalog.Category.NotFound"] = "لا يوجد تصنيف بهذا الكود.",
        ["Catalog.Category.NotActive"] = "التصنيف غير نشط.",
        ["Catalog.Category.ParentNotActive"] = "التصنيف الأب غير نشط.",
        ["Catalog.Category.Cycle"] = "التصنيف لا يمكن أن يكون تحت نفسه أو تحت أحد فروعه.",
        ["Catalog.Product.SkuTaken"] = "كود الصنف مستخدم بالفعل (وضع الإنشاء يضيف الجديد فقط).",
        ["Catalog.Product.NotFound"] = "لا يوجد صنف بهذا الكود هنا أو في ورقة الأصناف.",
        ["Catalog.Product.NotActive"] = "الصنف غير نشط.",
        ["Catalog.Product.TrackingNeedsStockItem"] = "التتبع بالتشغيلة أو السيريال للأصناف المخزنية فقط.",
        ["Catalog.ProductUnit.UnitAlreadyAdded"] = "الوحدة مضافة لهذا الصنف بالفعل (وضع الإنشاء يضيف الجديد فقط).",
        ["Catalog.ProductUnit.FactorMustBeWhole"] = "معامل التحويل يجب أن يكون عددًا صحيحًا لأن الوحدة الأساسية لا تقبل الكسور.",
        ["Catalog.ProductUnit.NotFound"] = "الصنف ليس له هذه الوحدة؛ أضفها في ورقة وحدات الأصناف أولًا.",
        ["Catalog.ProductUnit.NotActive"] = "وحدة الصنف غير نشطة.",
        ["Catalog.Barcode.Taken"] = "الباركود مستخدم بالفعل؛ لا تُعاد قيم الباركود أبدًا.",
    };

    public static string For(DocumentContext c, CatalogImportIssue issue) =>
        c.IsArabic && Arabic.TryGetValue(issue.Code, out var text) ? text : issue.Message;
}
