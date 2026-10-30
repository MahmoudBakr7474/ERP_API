using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Errors;

public static class ExportErrors
{
    /// <summary>The filters match more rows than one file may hold (Documents:MaxExportRows, fewer for PDF).</summary>
    public static Error TooManyRows(int maxRows) => Error.BusinessRule(
        "Organization.Export.TooManyRows",
        $"More than {maxRows} rows match. Narrow the filters, or choose Excel or CSV, which hold more rows than PDF.");
}
