using System.Globalization;
using System.Text.RegularExpressions;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Identity.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Purchasing.Infrastructure.ReadModels;

// Loads the names a page needs through the other modules' contracts: one call per kind, never per row.
internal sealed partial class ReferenceNames(
    IProductCatalog products, IWarehouseDirectory warehouses, IWarehouseLocationDirectory locations, IUserDirectory users)
{
    public async Task<NameLookup> LoadAsync(
        IEnumerable<Guid>? productIds = null, IEnumerable<Guid>? productUnitIds = null, IEnumerable<Guid>? warehouseIds = null,
        IEnumerable<Guid>? locationIds = null, IEnumerable<Guid?>? userIds = null, CancellationToken ct = default)
    {
        var productList = Distinct(productIds);
        var unitList = Distinct(productUnitIds);
        var warehouseList = Distinct(warehouseIds);
        var locationList = Distinct(locationIds);
        var userList = (userIds ?? []).OfType<Guid>().Distinct().ToList();

        return new NameLookup(
            productList.Count == 0 ? [] : (await products.FindManyAsync(productList, ct)).ToDictionary(p => p.Id),
            unitList.Count == 0 ? [] : (await products.FindUnitsAsync(unitList, ct)).ToDictionary(u => u.Id),
            warehouseList.Count == 0 ? [] : (await warehouses.FindManyAsync(warehouseList, ct)).ToDictionary(w => w.Id),
            locationList.Count == 0 ? [] : (await locations.FindManyAsync(locationList, ct)).ToDictionary(l => l.Id),
            userList.Count == 0 ? new Dictionary<Guid, string>() : await users.GetDisplayNamesAsync(userList, ct));
    }

    /// <summary>The draft number in a search such as "DPO-000123" or "123", or -1 when there is none.</summary>
    public static int DraftNumberOf(string search)
    {
        var match = DraftReference().Match(search.Trim());
        return match.Success && int.TryParse(match.Groups["number"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            ? number
            : -1;
    }

    private static List<Guid> Distinct(IEnumerable<Guid>? ids) => (ids ?? []).Distinct().ToList();

    [GeneratedRegex(@"^(?:D[A-Za-z]+-)?(?<number>\d{1,9})$", RegexOptions.CultureInvariant)]
    private static partial Regex DraftReference();
}
