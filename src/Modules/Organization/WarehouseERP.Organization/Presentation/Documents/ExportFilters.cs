using WarehouseERP.BuildingBlocks.Infrastructure.Documents;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Presentation.Documents;

// The filters a file was produced with, in its language, for its header and info sheet. Only filters that were
// actually set are listed; a status filter always is ("All" when none was chosen).
internal sealed class ExportFilters(DocumentContext c)
{
    private readonly List<ReportFilter> _filters = [];

    public ExportFilters Add(string labelAr, string labelEn, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            _filters.Add(new ReportFilter(c.L(labelAr, labelEn), value.Trim()));
        return this;
    }

    public ExportFilters Search(string? search) => Add("بحث", "Search", search);

    public ExportFilters Status(bool? isActive, string labelAr = "الحالة", string labelEn = "Status") =>
        Add(labelAr, labelEn, OrganizationLabels.StatusFilter(c, isActive));

    public ExportFilters Type(WarehouseType? type, string labelAr = "نوع المخزن", string labelEn = "Warehouse type") =>
        Add(labelAr, labelEn, type is { } t ? OrganizationLabels.Type(c, t) : null);

    public ExportFilters YesNo(string labelAr, string labelEn, bool? value) =>
        Add(labelAr, labelEn, value is { } v ? OrganizationLabels.YesNo(c, v) : null);

    public ExportFilters Named(string labelAr, string labelEn, string? code, string? nameAr, string? nameEn) =>
        Add(labelAr, labelEn, code is null ? null : nameAr is null ? code : OrganizationLabels.Named(c, code, nameAr, nameEn));

    public IReadOnlyList<ReportFilter> Build() => _filters;
}
