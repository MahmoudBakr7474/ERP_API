using System.Text.RegularExpressions;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Imports;
using WarehouseERP.Organization.Application.Contracts.Imports;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;

namespace WarehouseERP.Organization.Application.Features.Imports;

/// <summary>
/// Decides, for every row against the organization as it is, what confirming the import would do, and finds every
/// problem. The rules are the single-location endpoints' (ADR 0013): warehouse scope, an active warehouse for new
/// locations, codes unique per warehouse and fixed once created, names only for renames. The import adds what a file
/// needs and what bulk changes must never do: duplicates in the file, no transit warehouse (its locations belong to
/// stock transfers), and the default location left as it is. A location is always found by branch, warehouse and its
/// own code, so an import can never move a location to another warehouse.
/// </summary>
public static partial class LocationImportPlanner
{
    private static readonly StringComparer Keys = StringComparer.OrdinalIgnoreCase;

    // The single-location validator's rule: ASCII only (varchar(50)); dots allow bin codes such as A.01.02.
    [GeneratedRegex("^[A-Za-z0-9_.-]+$")]
    private static partial Regex LocationCodePattern();

    /// <summary>The branch codes a batch names: what <see cref="ILocationImportStore.LoadAsync"/> must load.</summary>
    public static IReadOnlyCollection<string> BranchCodes(LocationImportBatch batch) =>
        batch.Rows.Select(r => r.BranchCode).OfType<string>().Where(c => c.Length <= 30).Distinct(Keys).ToList();

    /// <param name="scope">The caller's warehouses; null when they may act on every warehouse.</param>
    public static LocationImportPlan Plan(
        LocationImportBatch batch, LocationImportMode mode, LocationImportSnapshot current, LocationImportPermissions can,
        IReadOnlySet<Guid>? scope)
    {
        var plan = new LocationImportPlan(mode);
        plan.Issues.AddRange(batch.FileIssues);
        if (batch.Rows.Count == 0 && batch.FileIssues.Count == 0)
            plan.Issues.Add(new LocationImportIssue(1, null, ImportIssueCodes.NoRows,
                "The file has no rows to import. Fill in the template's Locations sheet and keep its header row.", null));

        var branches = current.Branches.GroupBy(b => b.Code, Keys).ToDictionary(g => g.Key, g => g.First(), Keys);
        var warehouses = current.Warehouses.ToDictionary(w => (w.BranchId, w.Code.ToUpperInvariant()));
        var locations = current.Locations.ToDictionary(l => (l.WarehouseId, l.Code.ToUpperInvariant()));
        var duplicates = batch.Rows
            .Where(r => r.BranchCode is not null && r.WarehouseCode is not null && r.Code is not null)
            .GroupBy(Key, Keys).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet(Keys);

        foreach (var row in batch.Rows)
        {
            var check = new LocationRowCheck(plan.Issues, row.Row);
            var branchCode = check.Required("branchCode", row.BranchCode, 30);
            var warehouseCode = check.Required("warehouseCode", row.WarehouseCode, 30);
            var code = check.Code("code", row.Code, 50, LocationCodePattern(),
                "Only English letters, digits and . _ - are allowed in a location code.");
            var nameAr = check.Required("nameAr", row.NameAr, 150);
            var nameEn = check.Optional("nameEn", row.NameEn, 150);
            if (branchCode is not null && warehouseCode is not null && code is not null && duplicates.Contains(Key(row)))
            {
                check.Fail("code", ImportIssueCodes.DuplicateInFile, "The same location appears more than once in the file.", code);
                plan.Duplicates.Add(row.Row);
            }

            var warehouse = FindWarehouse(check, branchCode, warehouseCode, branches, warehouses, scope);
            var existing = warehouse is null || code is null ? null : locations.GetValueOrDefault((warehouse.Id, code.ToUpperInvariant()));
            var action = check.Failed || warehouse is null || code is null
                ? LocationImportAction.Error
                : Decide(check, mode, can, warehouse, existing, code, nameAr, nameEn);
            if (check.Failed)
                action = LocationImportAction.Error;

            plan.Locations.Add(new PlannedLocation(row.Row, action, warehouse, existing, code, nameAr, nameEn));
            plan.Rows.Add(new LocationImportRowResult(row.Row, $"{row.BranchCode}/{row.WarehouseCode}/{row.Code}", action));
        }
        return plan;
    }

    // The warehouse a row names, or null with its problem. A scoped user learns nothing about warehouses outside their
    // scope: a code they can't reach reads as access denied whether or not it exists, as on the single endpoints.
    private static Warehouse? FindWarehouse(
        LocationRowCheck check, string? branchCode, string? warehouseCode, Dictionary<string, Branch> branches,
        Dictionary<(Guid, string), Warehouse> warehouses, IReadOnlySet<Guid>? scope)
    {
        if (branchCode is null || warehouseCode is null)
            return null;
        var branch = branches.GetValueOrDefault(branchCode);
        var warehouse = branch is null ? null : warehouses.GetValueOrDefault((branch.Id, warehouseCode.ToUpperInvariant()));
        var named = $"{branchCode}/{warehouseCode}";
        if (scope is not null && (warehouse is null || !scope.Contains(warehouse.Id)))
            check.Fail("warehouseCode", WarehouseErrors.AccessDenied.Code, "You are not allowed to access this warehouse.", named);
        else if (branch is null)
            check.Fail("branchCode", BranchErrors.NotFound.Code, "No branch has this code.", branchCode);
        else if (warehouse is null)
            check.Fail("warehouseCode", WarehouseErrors.NotFound.Code, "The branch has no warehouse with this code.", named);
        else if (warehouse.WarehouseType == WarehouseType.Transit)
            check.Fail("warehouseCode", ImportIssueCodes.TransitWarehouse,
                "A transit warehouse's locations are opened and closed by stock transfers.", named);
        else
            return warehouse;
        return null;
    }

    private static LocationImportAction Decide(
        LocationRowCheck check, LocationImportMode mode, LocationImportPermissions can, Warehouse warehouse,
        WarehouseLocation? existing, string code, string? nameAr, string? nameEn)
    {
        if (existing is null)
        {
            if (string.Equals(code, Warehouse.DefaultLocationCode, StringComparison.OrdinalIgnoreCase))
                check.Fail("code", ImportIssueCodes.DefaultLocationFixed,
                    "DEFAULT is the code of the warehouse's default location, which comes with the warehouse.", code);
            else if (!warehouse.IsActive)
                check.Fail("warehouseCode", WarehouseDomainErrors.NotActive.Code, "The warehouse is inactive; it gets no new locations.",
                    warehouse.Code);
            else if (!can.Create)
                check.Fail(null, ImportIssueCodes.NotPermitted, "Adding locations needs the permission to create locations.");
            return check.Failed ? LocationImportAction.Error : LocationImportAction.Create;
        }

        switch (mode)
        {
            case LocationImportMode.Create:
                check.Fail("code", WarehouseLocationErrors.CodeTaken.Code,
                    "The warehouse already has a location with this code (Create mode adds new locations only).", code);
                return LocationImportAction.Error;
            case LocationImportMode.Skip:
                return LocationImportAction.Skip;
        }

        // Update: names only; an empty English name keeps the current one.
        if (existing.NameAr == nameAr && (nameEn is null || existing.NameEn == nameEn))
            return LocationImportAction.Unchanged;
        if (existing.IsDefault)
            check.Fail("code", ImportIssueCodes.DefaultLocationFixed,
                "The default location is never changed by an import; rename it on the warehouse screen.", code);
        else if (!can.Update)
            check.Fail(null, ImportIssueCodes.NotPermitted, "Renaming locations needs the permission to update locations.");
        return check.Failed ? LocationImportAction.Error : LocationImportAction.Update;
    }

    private static string Key(LocationImportRow row) => $"{row.BranchCode}\u001f{row.WarehouseCode}\u001f{row.Code}";
}
