using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Imports.CommitLocationImport;

/// <summary>
/// Applies a checked plan through the domain methods only (no SQL, no shortcut) without saving: the caller's single
/// save point commits it or nothing. New locations go through their warehouse (which refuses an inactive one), renames
/// through the location. A domain refusal here would mean the planner missed a rule; it is returned and nothing saved.
/// </summary>
public static class LocationImportWriter
{
    public static Result Apply(LocationImportPlan plan)
    {
        foreach (var row in plan.Locations)
        {
            if (row.Action == LocationImportAction.Create)
            {
                var added = row.Warehouse!.AddLocation(row.Code!, row.NameAr!, row.NameEn);
                if (added.IsFailure)
                    return added.Error;
            }
            else if (row.Action == LocationImportAction.Update)
                row.Existing!.Rename(row.NameAr!, row.NameEn ?? row.Existing.NameEn);
        }
        return Result.Success();
    }
}
