using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Application.Contracts.StockDocuments;
using WarehouseERP.Inventory.Application.Errors;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments;

// What a document refers to in Organization and Catalog, looked up through their contracts only: a physical active
// warehouse, active locations of it, stock items without batch or serial tracking and their active units. Drafts are
// checked when saved; postings check again after taking the warehouse lock, since anything may have changed since.
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

    /// <summary>The lines with their product units' facts; the DEFAULT location where none is named.</summary>
    public async Task<Result<IReadOnlyList<StockLineInput>>> ResolveLinesAsync(
        StockDocumentType documentType, Guid warehouseId, IReadOnlyList<StockLineRequest> lines, CancellationToken ct)
    {
        var productInfos = (await products.FindManyAsync(lines.Select(l => l.ProductId).ToList(), ct)).ToDictionary(p => p.Id);
        var units = (await products.FindUnitsAsync(lines.Select(l => l.ProductUnitId).ToList(), ct)).ToDictionary(u => u.Id);
        var named = lines.Where(l => l.LocationId.HasValue).Select(l => l.LocationId!.Value).ToList();
        var places = (await locations.FindManyAsync(named, ct)).ToDictionary(l => l.Id);
        var defaultLocation = await locations.FindDefaultAsync(warehouseId, ct);

        var resolved = new List<StockLineInput>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var number = i + 1;
            StockDirection direction;
            if (documentType == StockDocumentType.Adjustment)
            {
                if (line.Direction is not { } chosen)
                    return StockDocumentErrors.DirectionRequired(number);
                direction = chosen;
            }
            else
                direction = documentType == StockDocumentType.Issue ? StockDirection.Out : StockDirection.In;

            var product = CheckProduct(productInfos.GetValueOrDefault(line.ProductId), direction, number);
            if (product.IsFailure)
                return product.Error;
            if (!units.TryGetValue(line.ProductUnitId, out var unit) || unit.ProductId != line.ProductId)
                return StockDocumentErrors.ProductUnitNotFound(number);
            if (!unit.IsActive)
                return StockDocumentErrors.ProductUnitNotActive(number);

            var location = line.LocationId is { } locationId ? places.GetValueOrDefault(locationId) : defaultLocation;
            var place = CheckLocation(location, warehouseId, number);
            if (place.IsFailure)
                return place.Error;

            resolved.Add(new StockLineInput(
                line.ProductId, unit.Id, unit.UnitId, unit.ConversionFactor, unit.UnitAllowsFraction,
                unit.BaseUnitAllowsFraction, line.Quantity, direction, line.UnitCost, location!.Id));
        }
        return resolved;
    }

    /// <summary>
    /// Checks the document again inside its posting, after the warehouse lock: its warehouse, every location it moves
    /// stock through, and every product (an inactive product may still go out).
    /// </summary>
    public async Task<Result<PostingReferences>> CheckForPostingAsync(StockDocument document, CancellationToken ct)
    {
        var warehouse = await UsableWarehouseAsync(document.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;

        var locationIds = document.Lines
            .SelectMany(l => l.Allocations)
            .SelectMany(a => new[] { a.FromLocationId, a.ToLocationId })
            .OfType<Guid>()
            .ToList();
        var places = (await locations.FindManyAsync(locationIds, ct)).ToDictionary(l => l.Id);
        var productInfos = (await products.FindManyAsync(document.Lines.Select(l => l.ProductId).ToList(), ct)).ToDictionary(p => p.Id);

        foreach (var line in document.Lines)
        {
            var product = CheckProduct(productInfos.GetValueOrDefault(line.ProductId), line.Direction, line.LineNumber);
            if (product.IsFailure)
                return product.Error;
            foreach (var allocation in line.Allocations)
                foreach (var locationId in new[] { allocation.FromLocationId, allocation.ToLocationId }.OfType<Guid>())
                {
                    var place = CheckLocation(places.GetValueOrDefault(locationId), document.WarehouseId, line.LineNumber);
                    if (place.IsFailure)
                        return place.Error;
                }
        }
        return new PostingReferences(warehouse.Value, productInfos, places);
    }

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

    private static Result CheckLocation(WarehouseLocationInfo? location, Guid warehouseId, int line)
    {
        if (location is null || location.WarehouseId != warehouseId)
            return StockDocumentErrors.LocationNotFound(line);
        if (!location.IsActive)
            return StockDocumentErrors.LocationNotActive(line);
        return Result.Success();
    }
}
