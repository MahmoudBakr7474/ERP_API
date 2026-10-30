using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Exports;

/// <summary>
/// Exports read one row more than they may hold: that extra row means the file would be incomplete, so the user gets
/// a clear refusal instead of a silently truncated file or an unbounded one.
/// </summary>
public static class ExportLimit
{
    public static Result<IReadOnlyList<T>> Within<T>(IReadOnlyList<T> rows, int maxRows) =>
        rows.Count > maxRows ? ExportErrors.TooManyRows(maxRows) : Result.Success<IReadOnlyList<T>>(rows);
}
