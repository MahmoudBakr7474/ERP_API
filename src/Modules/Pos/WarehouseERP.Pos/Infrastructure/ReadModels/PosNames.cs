using WarehouseERP.Identity.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Pos.Infrastructure.ReadModels;

// Loads the names a receipt or a page needs through the other modules' contracts: one call per kind, never per row.
internal sealed class PosNames(
    ICompanyDirectory companies, IBranchDirectory branches, IWarehouseDirectory warehouses, IWarehouseLocationDirectory locations,
    IUserDirectory users)
{
    public async Task<PosNameLookup> LoadAsync(
        IEnumerable<Guid>? branchIds = null, IEnumerable<Guid>? warehouseIds = null, IEnumerable<Guid>? locationIds = null,
        IEnumerable<Guid?>? userIds = null, bool company = false, CancellationToken ct = default)
    {
        var branchList = (branchIds ?? []).Distinct().ToList();
        var warehouseList = (warehouseIds ?? []).Distinct().ToList();
        var locationList = (locationIds ?? []).Distinct().ToList();
        var userList = (userIds ?? []).OfType<Guid>().Distinct().ToList();
        return new PosNameLookup(
            company ? await companies.GetAsync(ct) : null,
            branchList.Count == 0 ? [] : (await branches.FindManyAsync(branchList, ct)).ToDictionary(b => b.Id),
            warehouseList.Count == 0 ? [] : (await warehouses.FindManyAsync(warehouseList, ct)).ToDictionary(w => w.Id),
            locationList.Count == 0 ? [] : (await locations.FindManyAsync(locationList, ct)).ToDictionary(l => l.Id),
            userList.Count == 0 ? new Dictionary<Guid, string>() : await users.GetDisplayNamesAsync(userList, ct));
    }
}
