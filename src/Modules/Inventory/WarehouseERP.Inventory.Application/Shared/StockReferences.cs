using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Contracts.Transfers;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Shared;

// What documents, transfers and counts refer to in Organization and Catalog, looked up through their contracts only:
// active warehouses (physical, except a transfer's transit side), active locations of them, stock items without batch
// or serial tracking and their active units. Drafts are checked when saved; postings check again after taking their
// warehouse locks, since anything may have changed since.
public sealed class StockReferences(
    IWarehouseDirectory warehouses, IWarehouseLocationDirectory locations, IProductCatalog products)
{
    /// <summary>The warehouse when ordinary documents may use it: it exists, is active and is physical.</summary>
    public async Task<Result<WarehouseInfo>> UsableWarehouseAsync(Guid warehouseId, CancellationToken ct)
    {
        var warehouse = await warehouses.FindAsync(warehouseId, ct);
        if (warehouse is null)
            return StockDocumentErrors.WarehouseNotFound;
        if (!warehouse.IsActive)
            return StockDocumentErrors.WarehouseNotActive;
        if (warehouse.Type == WarehouseType.Transit)
            return StockDocumentErrors.WarehouseIsTransit;
        return warehouse;
    }

    /// <summary>The transit warehouse a transfer goes through: the one named, or the source branch's only active one.</summary>
    public async Task<Result<WarehouseInfo>> TransitWarehouseAsync(Guid? transitWarehouseId, Guid sourceBranchId, CancellationToken ct)
    {
        if (transitWarehouseId is { } named)
        {
            var warehouse = await warehouses.FindAsync(named, ct);
            return warehouse is { Type: WarehouseType.Transit, IsActive: true } ? warehouse : StockTransferErrors.TransitWarehouseInvalid;
        }

        var inBranch = await warehouses.ListIdsInBranchesAsync([sourceBranchId], ct);
        var candidates = (await warehouses.FindManyAsync(inBranch, ct))
            .Where(w => w is { Type: WarehouseType.Transit, IsActive: true })
            .ToList();
        return candidates.Count == 1 ? candidates[0] : StockTransferErrors.TransitWarehouseRequired;
    }

    /// <summary>The lines with their product units' facts; the DEFAULT location where none is named.</summary>
    public async Task<Result<IReadOnlyList<StockLineInput>>> ResolveLinesAsync(
        StockDocumentType documentType, Guid warehouseId, IReadOnlyList<StockLineRequest> lines, CancellationToken ct)
    {
        var productInfos = (await products.FindManyAsync(lines.Select(l => l.ProductId).ToList(), ct)).ToDictionary(p => p.Id);
        var units = (await products.FindUnitsAsync(lines.Select(l => l.ProductUnitId).ToList(), ct)).ToDictionary(u => u.Id);
        var places = (await locations.FindManyAsync(lines.Where(l => l.LocationId.HasValue).Select(l => l.LocationId!.Value).ToList(), ct))
            .ToDictionary(l => l.Id);
        var defaultLocation = await locations.FindDefaultAsync(warehouseId, ct);

        var resolved = new List<StockLineInput>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var number = i + 1;
            StockDirection direction;
            if (documentType == StockDocumentType.Adjustment)
            {
                if (line.Direction is not ({ } chosen and (StockDirection.In or StockDirection.Out)))
                    return StockDocumentErrors.DirectionRequired(number);
                direction = chosen;
            }
            else
                direction = documentType is StockDocumentType.Issue or StockDocumentType.PurchaseReturn ? StockDirection.Out : StockDirection.In;

            var product = CheckProduct(productInfos.GetValueOrDefault(line.ProductId), direction, number);
            if (product.IsFailure)
                return product.Error;
            var unit = CheckUnit(units.GetValueOrDefault(line.ProductUnitId), line.ProductId, number);
            if (unit.IsFailure)
                return unit.Error;
            var location = line.LocationId is { } locationId ? places.GetValueOrDefault(locationId) : defaultLocation;
            var place = CheckLocation(location, warehouseId, number);
            if (place.IsFailure)
                return place.Error;

            resolved.Add(new StockLineInput(
                line.ProductId, unit.Value.Id, unit.Value.UnitId, unit.Value.ConversionFactor, unit.Value.UnitAllowsFraction,
                unit.Value.BaseUnitAllowsFraction, line.Quantity, direction, line.UnitCost, location!.Id));
        }
        return resolved;
    }

    /// <summary>
    /// A transfer's lines with their product units' facts, picked from the source's DEFAULT location where none is named.
    /// An inactive product may move: it is stock the company already holds.
    /// </summary>
    public async Task<Result<IReadOnlyList<TransferLineInput>>> ResolveTransferLinesAsync(
        Guid sourceWarehouseId, IReadOnlyList<TransferLineRequest> lines, CancellationToken ct)
    {
        var productInfos = (await products.FindManyAsync(lines.Select(l => l.ProductId).ToList(), ct)).ToDictionary(p => p.Id);
        var units = (await products.FindUnitsAsync(lines.Select(l => l.ProductUnitId).ToList(), ct)).ToDictionary(u => u.Id);
        var places = (await locations.FindManyAsync(
                lines.Where(l => l.SourceLocationId.HasValue).Select(l => l.SourceLocationId!.Value).ToList(), ct))
            .ToDictionary(l => l.Id);
        var defaultLocation = await locations.FindDefaultAsync(sourceWarehouseId, ct);

        var resolved = new List<TransferLineInput>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var number = i + 1;
            var product = CheckProduct(productInfos.GetValueOrDefault(line.ProductId), StockDirection.Move, number);
            if (product.IsFailure)
                return product.Error;
            var unit = CheckUnit(units.GetValueOrDefault(line.ProductUnitId), line.ProductId, number);
            if (unit.IsFailure)
                return unit.Error;
            var location = line.SourceLocationId is { } locationId ? places.GetValueOrDefault(locationId) : defaultLocation;
            var place = CheckLocation(location, sourceWarehouseId, number);
            if (place.IsFailure)
                return place.Error;

            resolved.Add(new TransferLineInput(
                line.ProductId, unit.Value.Id, unit.Value.UnitId, unit.Value.ConversionFactor, unit.Value.UnitAllowsFraction,
                unit.Value.BaseUnitAllowsFraction, line.Quantity, location!.Id));
        }
        return resolved;
    }

    /// <summary>Each product's base product unit, which lines generated in base units use.</summary>
    public async Task<IReadOnlyDictionary<Guid, ProductUnitInfo>> BaseUnitsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken ct) =>
        (await products.FindBaseUnitsAsync(productIds, ct)).ToDictionary(u => u.ProductId);

    /// <summary>The products when every one of them is a countable stock item (in entry order for error messages).</summary>
    public async Task<Result<IReadOnlyDictionary<Guid, ProductInfo>>> CountableProductsAsync(IReadOnlyList<Guid> productIds, CancellationToken ct)
    {
        var productInfos = (await products.FindManyAsync(productIds, ct)).ToDictionary(p => p.Id);
        for (var i = 0; i < productIds.Count; i++)
        {
            var product = CheckProduct(productInfos.GetValueOrDefault(productIds[i]), StockDirection.Move, i + 1);
            if (product.IsFailure)
                return product.Error;
        }
        return productInfos;
    }

    /// <summary>The active locations of the warehouse among these ids; anything else is refused with its line number.</summary>
    public async Task<Result<IReadOnlyDictionary<Guid, WarehouseLocationInfo>>> WarehouseLocationsAsync(
        Guid warehouseId, IReadOnlyList<Guid> locationIds, CancellationToken ct)
    {
        var places = (await locations.FindManyAsync(locationIds, ct)).ToDictionary(l => l.Id);
        for (var i = 0; i < locationIds.Count; i++)
            if (places.GetValueOrDefault(locationIds[i]) is not { IsActive: true } place || place.WarehouseId != warehouseId)
                return StockCountErrors.LocationNotFound(i + 1);
        return places;
    }

    public Task<WarehouseLocationInfo?> DefaultLocationAsync(Guid warehouseId, CancellationToken ct) =>
        locations.FindDefaultAsync(warehouseId, ct);

    /// <summary>
    /// Checks the document again inside its posting, after the warehouse locks: every warehouse it touches (physical, but
    /// for a transfer's transit side), every location it moves stock through, and every product (an inactive product may
    /// still go out or move).
    /// </summary>
    public async Task<Result<PostingReferences>> CheckForPostingAsync(StockDocument document, CancellationToken ct)
    {
        var placesOfDocument = PlacesOf(document);
        var warehouseIds = placesOfDocument.Select(p => p.WarehouseId).Append(document.WarehouseId).Distinct().ToList();
        var warehouseInfos = (await warehouses.FindManyAsync(warehouseIds, ct)).ToDictionary(w => w.Id);
        foreach (var warehouseId in warehouseIds)
        {
            var warehouse = warehouseInfos.GetValueOrDefault(warehouseId);
            if (warehouse is null)
                return StockDocumentErrors.WarehouseNotFound;
            if (!warehouse.IsActive)
                return StockDocumentErrors.WarehouseNotActive;
            if (warehouse.Type == WarehouseType.Transit && document.TransferId is null)
                return StockDocumentErrors.WarehouseIsTransit;
        }

        var places = (await locations.FindManyAsync(placesOfDocument.Select(p => p.LocationId).Distinct().ToList(), ct))
            .ToDictionary(l => l.Id);
        var productInfos = (await products.FindManyAsync(document.Lines.Select(l => l.ProductId).ToList(), ct)).ToDictionary(p => p.Id);
        foreach (var line in document.Lines)
        {
            var product = CheckProduct(productInfos.GetValueOrDefault(line.ProductId), line.Direction, line.LineNumber);
            if (product.IsFailure)
                return product.Error;
            foreach (var place in line.Allocations.SelectMany(Sides))
            {
                var checkedPlace = CheckLocation(places.GetValueOrDefault(place.LocationId), place.WarehouseId, line.LineNumber);
                if (checkedPlace.IsFailure)
                    return checkedPlace.Error;
            }
        }
        return new PostingReferences(warehouseInfos, productInfos, places, placesOfDocument);
    }

    /// <summary>Every place a document takes stock from or puts stock into.</summary>
    public static IReadOnlyList<StockPlace> PlacesOf(StockDocument document) =>
        document.Lines.SelectMany(l => l.Allocations).SelectMany(Sides).Distinct().ToList();

    private static IEnumerable<StockPlace> Sides(StockAllocation allocation)
    {
        if (allocation.FromLocationId is { } from)
            yield return new StockPlace(allocation.FromWarehouseId!.Value, from);
        if (allocation.ToLocationId is { } to)
            yield return new StockPlace(allocation.ToWarehouseId!.Value, to);
    }

    // Only stock coming into the company (In) needs an active product; going out or moving between locations does not.
    private static Result CheckProduct(ProductInfo? product, StockDirection direction, int line)
    {
        if (product is null)
            return StockDocumentErrors.ProductNotFound(line);
        if (product.Type != ProductType.StockItem)
            return StockDocumentErrors.ProductNotStockItem(line);
        if (product.TrackingMode != TrackingMode.None)
            return StockDocumentErrors.TrackingNotSupported(line);
        if (direction == StockDirection.In && !product.IsActive)
            return StockDocumentErrors.ProductNotActive(line);
        return Result.Success();
    }

    private static Result<ProductUnitInfo> CheckUnit(ProductUnitInfo? unit, Guid productId, int line)
    {
        if (unit is null || unit.ProductId != productId)
            return StockDocumentErrors.ProductUnitNotFound(line);
        if (!unit.IsActive)
            return StockDocumentErrors.ProductUnitNotActive(line);
        return unit;
    }

    private static Result CheckLocation(WarehouseLocationInfo? location, Guid warehouseId, int line)
    {
        if (location is null || location.WarehouseId != warehouseId)
            return StockDocumentErrors.LocationNotFound(line);
        if (!location.IsActive)
            return StockDocumentErrors.LocationNotActive(line);
        return Result.Success();
    }
}
