using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Presentation.Documents;

// Organization words as documents print them, the same terms as the web app's translations.
internal static class OrganizationLabels
{
    public static string Type(DocumentContext c, WarehouseType type) =>
        type == WarehouseType.Transit ? c.L("ترانزيت", "Transit") : c.L("فعلي", "Physical");

    public static string Status(DocumentContext c, bool isActive) => isActive ? c.L("نشط", "Active") : c.L("غير نشط", "Inactive");

    /// <summary>"Active", "Inactive" or "All" for a status filter.</summary>
    public static string StatusFilter(DocumentContext c, bool? isActive) =>
        isActive is { } active ? Status(c, active) : c.L("الكل", "All");

    public static string YesNo(DocumentContext c, bool value) => value ? c.L("نعم", "Yes") : c.L("لا", "No");

    public static string Entity(DocumentContext c, OrganizationEntity entity) => entity switch
    {
        OrganizationEntity.Company => c.L("الشركة", "Company"),
        OrganizationEntity.Branch => c.L("فرع", "Branch"),
        OrganizationEntity.Warehouse => c.L("مخزن", "Warehouse"),
        _ => c.L("موقع", "Location"),
    };

    public static string Issue(DocumentContext c, OrganizationDataIssue issue) => issue switch
    {
        OrganizationDataIssue.CompanyMissingTaxNumber => c.L("الشركة بدون رقم ضريبي", "Company has no tax registration number"),
        OrganizationDataIssue.CompanyMissingCommercialRegistration => c.L("الشركة بدون رقم سجل تجاري", "Company has no commercial registration"),
        OrganizationDataIssue.CompanyMissingAddress => c.L("عنوان الشركة غير مكتمل", "Company address incomplete"),
        OrganizationDataIssue.CompanyMissingContact => c.L("الشركة بدون هاتف أو بريد", "Company has no phone or e-mail"),
        OrganizationDataIssue.MissingEnglishName => c.L("بدون اسم إنجليزي", "No English name"),
        OrganizationDataIssue.BranchWithoutActiveWarehouse => c.L("فرع نشط بدون مخزن نشط", "Active branch without an active warehouse"),
        OrganizationDataIssue.ActiveWarehouseInInactiveBranch => c.L("مخزن نشط في فرع غير نشط", "Active warehouse in an inactive branch"),
        OrganizationDataIssue.NoDefaultLocation => c.L("مخزن بدون موقع افتراضي", "Warehouse without a default location"),
        OrganizationDataIssue.InactiveDefaultLocation => c.L("الموقع الافتراضي غير نشط", "Default location inactive"),
        _ => c.L("لا يوجد مخزن ترانزيت واحد للتحويلات", "No single transit warehouse for transfers"),
    };

    /// <summary>
    /// A record's code and its name in the document's language, as filters and tables print it. Arabic documents put the
    /// name first: the value is then one right-to-left paragraph, so a long name wraps whole and the code ends it (code
    /// first would make it a left-to-right paragraph whose wrapped Arabic lines are split around the code).
    /// </summary>
    public static string Named(DocumentContext c, string code, string nameAr, string? nameEn) =>
        c.IsArabic ? $"{c.Name(nameAr, nameEn)} · {code}" : $"{code} · {c.Name(nameAr, nameEn)}";
}
