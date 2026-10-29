using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Pos.Infrastructure.ReadModels;

// Display names for one page or receipt. Rows never lose their branch, warehouse or location (no hard deletes), so "?"
// only shows if something is badly wrong.
internal sealed class PosNameLookup(
    CompanyInfo? company,
    IReadOnlyDictionary<Guid, BranchInfo> branches,
    IReadOnlyDictionary<Guid, WarehouseInfo> warehouses,
    IReadOnlyDictionary<Guid, WarehouseLocationInfo> locations,
    IReadOnlyDictionary<Guid, string> users)
{
    private const string Unknown = "?";

    public string CompanyName => company?.NameAr ?? Unknown;

    public string TimeZoneId => company?.TimeZoneId ?? "UTC";

    public string CurrencyCode => company?.BaseCurrencyCode ?? Unknown;

    public string BranchCode(Guid branchId) => branches.TryGetValue(branchId, out var b) ? b.Code : Unknown;

    public string BranchName(Guid branchId) => branches.TryGetValue(branchId, out var b) ? b.NameAr : Unknown;

    public string WarehouseCode(Guid warehouseId) => warehouses.TryGetValue(warehouseId, out var w) ? w.Code : Unknown;

    public string LocationCode(Guid locationId) => locations.TryGetValue(locationId, out var l) ? l.Code : Unknown;

    public string? UserName(Guid? userId) => userId is { } id && users.TryGetValue(id, out var name) ? name : null;
}
