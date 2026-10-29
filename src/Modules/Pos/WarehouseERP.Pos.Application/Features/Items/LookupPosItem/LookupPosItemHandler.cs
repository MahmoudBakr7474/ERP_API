using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Pos.Application.Contracts.Items;
using WarehouseERP.Pos.Application.Errors;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Items.LookupPosItem;

// A scan: only an active barcode on an active unit of an active product is found (Catalog), then priced and stocked for
// the terminal.
public sealed class LookupPosItemHandler(TillAccess tills, IProductCatalog products, PosItems items)
    : IQueryHandler<LookupPosItemQuery, PosItemResponse>
{
    public async Task<Result<PosItemResponse>> HandleAsync(LookupPosItemQuery query, CancellationToken ct)
    {
        var terminal = await tills.UsableAsync(query.TerminalId, ct);
        if (terminal.IsFailure)
            return terminal.Error;
        var unit = await products.FindUnitByBarcodeAsync(query.Barcode.Trim(), ct);
        if (unit is null || await products.FindAsync(unit.ProductId, ct) is not { } product)
            return ItemErrors.NotFound;
        var described = await items.DescribeAsync(terminal.Value, [new PosUnit(product, unit)], ct);
        return described.IsFailure ? described.Error : described.Value[0];
    }
}
