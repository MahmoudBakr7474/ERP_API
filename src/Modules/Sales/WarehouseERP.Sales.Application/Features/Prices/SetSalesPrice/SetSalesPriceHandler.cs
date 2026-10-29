using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Prices;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Prices.SetSalesPrice;

// One price per product unit: set creates it, changes it, or brings back a removed one. The product unit must be an active
// unit of an active product. Lines already priced keep their snapshot. The temporal history keeps every earlier price.
public sealed partial class SetSalesPriceHandler(
    ISalesPriceRepository prices, IProductCatalog products, ILogger<SetSalesPriceHandler> logger)
    : ICommandHandler<SetSalesPriceCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetSalesPriceCommand command, CancellationToken ct)
    {
        var unit = (await products.FindUnitsAsync([command.ProductUnitId], ct)).SingleOrDefault();
        if (unit is null)
            return PriceErrors.ProductUnitNotFound;
        if (!unit.IsActive || await products.FindAsync(unit.ProductId, ct) is not { IsActive: true })
            return PriceErrors.ProductUnitNotActive;

        var price = await prices.GetForUpdateAsync(command.ProductUnitId, ct);
        if (price is null)
        {
            var created = SalesPrice.Create(unit.ProductId, unit.Id, command.UnitPrice);
            if (created.IsFailure)
                return created.Error;
            prices.Add(created.Value);
        }
        else
        {
            var set = price.Set(command.UnitPrice);
            if (set.IsFailure)
                return set.Error;
        }
        var saved = await prices.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        PriceSet(logger, command.ProductUnitId, command.UnitPrice);
        return Done.Value;
    }

    [LoggerMessage(EventId = 7011, Level = LogLevel.Information, Message = "Sales price of product unit {ProductUnitId} set to {UnitPrice}")]
    private static partial void PriceSet(ILogger logger, Guid productUnitId, decimal unitPrice);
}
