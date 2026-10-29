using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Pos.Application.Contracts.Items;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Items.SearchPosItems;

// Searching active products by SKU, name or barcode; every active unit of each comes back (the cashier picks the unit).
public sealed class SearchPosItemsHandler(TillAccess tills, IProductCatalog products, PosItems items)
    : IQueryHandler<SearchPosItemsQuery, IReadOnlyList<PosItemResponse>>
{
    private const int DefaultTake = 20;

    public async Task<Result<IReadOnlyList<PosItemResponse>>> HandleAsync(SearchPosItemsQuery query, CancellationToken ct)
    {
        var terminal = await tills.UsableAsync(query.TerminalId, ct);
        if (terminal.IsFailure)
            return terminal.Error;
        var found = await products.SearchActiveAsync(query.Search, query.Take ?? DefaultTake, ct);
        var units = (await products.FindUnitsOfProductsAsync(found.Select(p => p.Id).ToList(), ct))
            .Where(u => u.IsActive)
            .ToLookup(u => u.ProductId);
        var pairs = found.SelectMany(p => units[p.Id].OrderBy(u => u.ConversionFactor).Select(u => new PosUnit(p, u))).ToList();
        return await items.DescribeAsync(terminal.Value, pairs, ct);
    }
}
