using System.Globalization;
using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Organization.Application.Contracts.Branches;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Contracts.Warehouses;
using WarehouseERP.Organization.Application.Features.Exports.ExportStructure;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Presentation.Documents;

// The organization directories and reports as rows and columns. Excel and CSV get analysis-friendly columns (codes and
// names apart, both names); PDF gets the columns that fit a printed page. Values come from the Application queries
// unchanged; the only sums are of the listed rows' own counts.
internal static class OrganizationReportDocuments
{
    public static TabularReport Branches(
        DocumentContext c, IReadOnlyList<BranchSummaryResponse> rows, ExportFormat format, IReadOnlyList<ReportFilter> filters)
    {
        var title = c.L("دليل الفروع", "Branch directory");
        var summary = StatusSummary(c, rows.Count(b => b.IsActive), rows.Count(b => !b.IsActive));
        var totals = (All: rows.Sum(b => b.WarehouseCount), Active: rows.Sum(b => b.ActiveWarehouseCount));
        if (format == ExportFormat.Pdf)
            return new TabularReport(c.Language, "organization-branches", title,
                [
                    new(c.L("كود الفرع", "Branch code"), ColumnKind.Code, 1.1f),
                    new(c.L("الاسم العربي", "Arabic name"), ColumnKind.Text, 2.4f),
                    new(c.L("الاسم الإنجليزي", "English name"), ColumnKind.Text, 2f),
                    new(c.L("المخازن", "Warehouses"), ColumnKind.Count, 1.1f),
                    new(c.L("مخازن نشطة", "Active warehouses"), ColumnKind.Count, 1.1f),
                    new(c.L("الحالة", "Status"), ColumnKind.Text, 0.8f),
                ],
                rows.Select(b => Row(b.Code, b.NameAr, b.NameEn, b.WarehouseCount, b.ActiveWarehouseCount, OrganizationLabels.Status(c, b.IsActive))).ToList(),
                filters, Summary: summary, Totals: Row(c.L("الإجمالي", "Total"), null, null, totals.All, totals.Active, null));

        return new TabularReport(c.Language, "organization-branches", title,
            [
                new(c.L("كود الفرع", "Branch code"), ColumnKind.Code),
                new(c.L("الاسم العربي", "Arabic name")),
                new(c.L("الاسم الإنجليزي", "English name")),
                new(c.L("الحالة", "Status")),
                new(c.L("المخازن", "Warehouses"), ColumnKind.Count),
                new(c.L("مخازن نشطة", "Active warehouses"), ColumnKind.Count),
            ],
            rows.Select(b => Row(b.Code, b.NameAr, b.NameEn, OrganizationLabels.Status(c, b.IsActive), b.WarehouseCount, b.ActiveWarehouseCount)).ToList(),
            filters, Summary: summary, Totals: Row(c.L("الإجمالي", "Total"), null, null, null, totals.All, totals.Active));
    }

    public static TabularReport Warehouses(
        DocumentContext c, IReadOnlyList<WarehouseSummaryResponse> rows, ExportFormat format, IReadOnlyList<ReportFilter> filters)
    {
        var title = c.L("دليل المخازن", "Warehouse directory");
        List<ReportFilter> summary =
        [
            .. StatusSummary(c, rows.Count(w => w.IsActive), rows.Count(w => !w.IsActive)),
            new(c.L("فعلي", "Physical"), Count(rows.Count(w => w.Type == WarehouseType.Physical))),
            new(c.L("ترانزيت", "Transit"), Count(rows.Count(w => w.Type == WarehouseType.Transit))),
            new(c.L("نشطة تسمح بالبيع", "Active, selling"), Count(rows.Count(w => w.IsActive && w.AllowsSelling))),
        ];
        string? Default(WarehouseSummaryResponse w) => w.DefaultLocationCode is null
            ? c.L("لا يوجد", "None")
            : w.DefaultLocationIsActive == false ? $"{w.DefaultLocationCode} ({OrganizationLabels.Status(c, false)})" : w.DefaultLocationCode;
        if (format == ExportFormat.Pdf)
            return new TabularReport(c.Language, "organization-warehouses", title,
                [
                    new(c.L("الفرع", "Branch"), ColumnKind.Text, 1.8f),
                    new(c.L("كود المخزن", "Warehouse code"), ColumnKind.Code, 1.1f),
                    new(c.L("المخزن", "Warehouse"), ColumnKind.Text, 2.6f),
                    new(c.L("النوع", "Type"), ColumnKind.Text, 0.8f),
                    new(c.L("يسمح بالبيع", "Allows selling"), ColumnKind.Text, 0.8f),
                    new(c.L("الموقع الافتراضي", "Default location"), ColumnKind.Code, 1.2f),
                    new(c.L("المواقع", "Locations"), ColumnKind.Count, 0.75f),
                    new(c.L("مواقع نشطة", "Active locations"), ColumnKind.Count, 0.8f),
                    new(c.L("الحالة", "Status"), ColumnKind.Text, 0.75f),
                ],
                rows.Select(w => Row(OrganizationLabels.Named(c, w.BranchCode, w.BranchNameAr, w.BranchNameEn), w.Code, c.Name(w.NameAr, w.NameEn),
                    OrganizationLabels.Type(c, w.Type), w.AllowsSelling, Default(w), w.LocationCount, w.ActiveLocationCount,
                    OrganizationLabels.Status(c, w.IsActive))).ToList(),
                filters, Landscape: true, Summary: summary);

        return new TabularReport(c.Language, "organization-warehouses", title,
            [
                new(c.L("كود الفرع", "Branch code"), ColumnKind.Code),
                new(c.L("الفرع", "Branch")),
                new(c.L("كود المخزن", "Warehouse code"), ColumnKind.Code),
                new(c.L("الاسم العربي", "Arabic name")),
                new(c.L("الاسم الإنجليزي", "English name")),
                new(c.L("النوع", "Type")),
                new(c.L("يسمح بالبيع", "Allows selling")),
                new(c.L("الحالة", "Status")),
                new(c.L("حالة الفرع", "Branch status")),
                new(c.L("الموقع الافتراضي", "Default location"), ColumnKind.Code),
                new(c.L("حالة الموقع الافتراضي", "Default location status")),
                new(c.L("المواقع", "Locations"), ColumnKind.Count),
                new(c.L("مواقع نشطة", "Active locations"), ColumnKind.Count),
            ],
            rows.Select(w => Row(w.BranchCode, c.Name(w.BranchNameAr, w.BranchNameEn), w.Code, w.NameAr, w.NameEn,
                OrganizationLabels.Type(c, w.Type), w.AllowsSelling, OrganizationLabels.Status(c, w.IsActive),
                OrganizationLabels.Status(c, w.BranchIsActive), w.DefaultLocationCode,
                w.DefaultLocationIsActive is { } active ? OrganizationLabels.Status(c, active) : null,
                w.LocationCount, w.ActiveLocationCount)).ToList(),
            filters, Summary: summary);
    }

    public static TabularReport Locations(
        DocumentContext c, IReadOnlyList<LocationDirectoryResponse> rows, ExportFormat format, IReadOnlyList<ReportFilter> filters)
    {
        var title = c.L("دليل مواقع التخزين", "Storage location directory");
        List<ReportFilter> summary =
        [
            .. StatusSummary(c, rows.Count(l => l.IsActive), rows.Count(l => !l.IsActive)),
            new(c.L("المخازن", "Warehouses"), Count(rows.Select(l => l.WarehouseId).Distinct().Count())),
        ];
        if (format == ExportFormat.Pdf)
            return new TabularReport(c.Language, "organization-locations", title,
                [
                    new(c.L("الفرع", "Branch"), ColumnKind.Code, 0.9f),
                    new(c.L("المخزن", "Warehouse"), ColumnKind.Text, 2.2f),
                    new(c.L("كود الموقع", "Location code"), ColumnKind.Code, 1.2f),
                    new(c.L("الاسم العربي", "Arabic name"), ColumnKind.Text, 2.3f),
                    new(c.L("الاسم الإنجليزي", "English name"), ColumnKind.Text, 2f),
                    new(c.L("افتراضي", "Default"), ColumnKind.Text, 0.65f),
                    new(c.L("الحالة", "Status"), ColumnKind.Text, 0.75f),
                ],
                rows.Select(l => Row(l.BranchCode, OrganizationLabels.Named(c, l.WarehouseCode, l.WarehouseNameAr, l.WarehouseNameEn), l.Code,
                    l.NameAr, l.NameEn, l.IsDefault, OrganizationLabels.Status(c, l.IsActive))).ToList(),
                filters, Landscape: true, Summary: summary);

        return new TabularReport(c.Language, "organization-locations", title,
            [
                new(c.L("كود الفرع", "Branch code"), ColumnKind.Code),
                new(c.L("الفرع", "Branch")),
                new(c.L("كود المخزن", "Warehouse code"), ColumnKind.Code),
                new(c.L("المخزن", "Warehouse")),
                new(c.L("نوع المخزن", "Warehouse type")),
                new(c.L("كود الموقع", "Location code"), ColumnKind.Code),
                new(c.L("الاسم العربي", "Arabic name")),
                new(c.L("الاسم الإنجليزي", "English name")),
                new(c.L("افتراضي", "Default")),
                new(c.L("الحالة", "Status")),
                new(c.L("حالة المخزن", "Warehouse status")),
                new(c.L("تاريخ الإنشاء", "Created"), ColumnKind.DateTime),
            ],
            rows.Select(l => Row(l.BranchCode, c.Name(l.BranchNameAr, l.BranchNameEn), l.WarehouseCode, c.Name(l.WarehouseNameAr, l.WarehouseNameEn),
                OrganizationLabels.Type(c, l.WarehouseType), l.Code, l.NameAr, l.NameEn, l.IsDefault, OrganizationLabels.Status(c, l.IsActive),
                OrganizationLabels.Status(c, l.WarehouseIsActive), c.ToLocal(l.CreatedAtUtc))).ToList(),
            filters, Summary: summary);
    }

    // Branch, then its warehouses, then (when asked) each warehouse's locations. On paper the tree shows as a dash per
    // level; spreadsheets carry the branch and warehouse codes on every row to filter on.
    public static TabularReport Structure(
        DocumentContext c, OrganizationStructureExport export, ExportFormat format, IReadOnlyList<ReportFilter> filters)
    {
        var title = c.L("الهيكل التنظيمي", "Organizational structure");
        var pdf = format == ExportFormat.Pdf;
        var locations = export.Locations.ToLookup(l => l.WarehouseId);
        var rows = new List<IReadOnlyList<object?>>();
        // The dashes go on the reading start of the document: before a name that reads like the document, after one that
        // reads the other way (an Arabic-only name in an English document), whose paragraph runs right to left.
        string Indent(int level, string name)
        {
            if (!pdf)
                return name;
            var dashes = string.Join(" ", Enumerable.Repeat("–", level));
            return !c.IsArabic && BrandedPdf.StartsRightToLeft(name) ? $"{name} {dashes}" : $"{dashes} {name}";
        }
        foreach (var b in export.Branches)
        {
            var branchLocations = b.Warehouses.Sum(w => w.LocationCount);
            rows.Add(pdf
                ? Row(c.L("فرع", "Branch"), b.Code, c.Name(b.NameAr, b.NameEn), $"{c.L("مخازن", "Warehouses")}: {Count(b.Warehouses.Count)}",
                    branchLocations, OrganizationLabels.Status(c, b.IsActive))
                : Row(c.L("فرع", "Branch"), b.Code, null, b.Code, b.NameAr, b.NameEn, null, null, null, branchLocations,
                    OrganizationLabels.Status(c, b.IsActive)));
            foreach (var w in b.Warehouses)
            {
                var details = OrganizationLabels.Type(c, w.Type) + (w.AllowsSelling ? " · " + c.L("يبيع", "sells") : "");
                rows.Add(pdf
                    ? Row(c.L("مخزن", "Warehouse"), w.Code, Indent(1, c.Name(w.NameAr, w.NameEn)), details, w.LocationCount,
                        OrganizationLabels.Status(c, w.IsActive))
                    : Row(c.L("مخزن", "Warehouse"), b.Code, w.Code, w.Code, w.NameAr, w.NameEn, OrganizationLabels.Type(c, w.Type),
                        w.AllowsSelling, null, w.LocationCount, OrganizationLabels.Status(c, w.IsActive)));
                foreach (var l in locations[w.Id])
                    rows.Add(pdf
                        ? Row(c.L("موقع", "Location"), l.Code, Indent(2, c.Name(l.NameAr, l.NameEn)),
                            l.IsDefault ? c.L("افتراضي", "Default") : null, null, OrganizationLabels.Status(c, l.IsActive))
                        : Row(c.L("موقع", "Location"), b.Code, w.Code, l.Code, l.NameAr, l.NameEn, null, null, l.IsDefault, null,
                            OrganizationLabels.Status(c, l.IsActive)));
            }
        }

        List<ReportFilter> summary =
        [
            new(c.L("الفروع", "Branches"), Count(export.Branches.Count)),
            new(c.L("المخازن", "Warehouses"), Count(export.Branches.Sum(b => b.Warehouses.Count))),
            new(c.L("المواقع", "Locations"), Count(export.Branches.Sum(b => b.Warehouses.Sum(w => w.LocationCount)))),
        ];
        if (pdf)
            return new TabularReport(c.Language, "organization-structure", title,
                [
                    new(c.L("المستوى", "Level"), ColumnKind.Text, 1.1f),
                    new(c.L("الكود", "Code"), ColumnKind.Code, 1.2f),
                    new(c.L("الاسم", "Name"), ColumnKind.Text, 3.1f),
                    new(c.L("التفاصيل", "Details"), ColumnKind.Text, 1.3f),
                    new(c.L("المواقع", "Locations"), ColumnKind.Count, 0.8f),
                    new(c.L("الحالة", "Status"), ColumnKind.Text, 0.8f),
                ],
                rows, filters, Summary: summary);

        return new TabularReport(c.Language, "organization-structure", title,
            [
                new(c.L("المستوى", "Level")),
                new(c.L("كود الفرع", "Branch code"), ColumnKind.Code),
                new(c.L("كود المخزن", "Warehouse code"), ColumnKind.Code),
                new(c.L("الكود", "Code"), ColumnKind.Code),
                new(c.L("الاسم العربي", "Arabic name")),
                new(c.L("الاسم الإنجليزي", "English name")),
                new(c.L("نوع المخزن", "Warehouse type")),
                new(c.L("يسمح بالبيع", "Allows selling")),
                new(c.L("افتراضي", "Default")),
                new(c.L("المواقع", "Locations"), ColumnKind.Count),
                new(c.L("الحالة", "Status")),
            ],
            rows, filters, Summary: summary);
    }

    public static TabularReport DataIssues(
        DocumentContext c, IReadOnlyList<OrganizationDataIssueResponse> rows, ExportFormat format, IReadOnlyList<ReportFilter> filters)
    {
        var title = c.L("تقرير جودة بيانات الهيكل التنظيمي", "Organization data-quality report");
        string? Detail(OrganizationDataIssueResponse i) => i.Issue == OrganizationDataIssue.NoDefaultTransitWarehouse
            ? c.L($"مخازن ترانزيت نشطة: {i.Detail}", $"Active transit warehouses: {i.Detail}")
            : i.Detail;
        var summary = Enum.GetValues<OrganizationDataIssue>()
            .Select(issue => (issue, count: rows.Count(i => i.Issue == issue)))
            .Where(x => x.count > 0)
            .Select(x => new ReportFilter(OrganizationLabels.Issue(c, x.issue), Count(x.count)))
            .ToList();
        if (format == ExportFormat.Pdf)
            return new TabularReport(c.Language, "organization-data-quality", title,
                [
                    new(c.L("الملاحظة", "Issue"), ColumnKind.Text, 2.2f),
                    new(c.L("السجل", "Record"), ColumnKind.Text, 0.8f),
                    new(c.L("الفرع", "Branch"), ColumnKind.Code, 0.9f),
                    new(c.L("المخزن", "Warehouse"), ColumnKind.Code, 0.9f),
                    new(c.L("الكود", "Code"), ColumnKind.Code, 1.1f),
                    new(c.L("الاسم", "Name"), ColumnKind.Text, 2.4f),
                    new(c.L("التفاصيل", "Detail"), ColumnKind.Text, 1.4f),
                ],
                rows.Select(i => Row(OrganizationLabels.Issue(c, i.Issue), OrganizationLabels.Entity(c, i.Entity), i.BranchCode, i.WarehouseCode,
                    i.Code, i.NameAr is null ? null : c.Name(i.NameAr, i.NameEn), Detail(i))).ToList(),
                filters, Landscape: true, Summary: summary);

        return new TabularReport(c.Language, "organization-data-quality", title,
            [
                new(c.L("الملاحظة", "Issue")),
                new(c.L("السجل", "Record")),
                new(c.L("كود الفرع", "Branch code"), ColumnKind.Code),
                new(c.L("كود المخزن", "Warehouse code"), ColumnKind.Code),
                new(c.L("الكود", "Code"), ColumnKind.Code),
                new(c.L("الاسم العربي", "Arabic name")),
                new(c.L("الاسم الإنجليزي", "English name")),
                new(c.L("التفاصيل", "Detail")),
            ],
            rows.Select(i => Row(OrganizationLabels.Issue(c, i.Issue), OrganizationLabels.Entity(c, i.Entity), i.BranchCode, i.WarehouseCode,
                i.Code, i.NameAr, i.NameEn, Detail(i))).ToList(),
            filters, Summary: summary);
    }

    private static List<ReportFilter> StatusSummary(DocumentContext c, int active, int inactive) =>
    [
        new(c.L("نشط", "Active"), Count(active)),
        new(c.L("غير نشط", "Inactive"), Count(inactive)),
    ];

    private static string Count(int value) => value.ToString("#,##0", CultureInfo.InvariantCulture);

    private static object?[] Row(params object?[] values) => values;
}
