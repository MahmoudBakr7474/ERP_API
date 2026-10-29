using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.ProductTaxes;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.ProductTaxes.SetProductTax;

// One classification per product: set creates it, changes it, or brings back a removed one. The product must be active.
// POS sales already made keep the treatment and rate they were sold with. The temporal history keeps every earlier one.
public sealed partial class SetProductTaxHandler(
    IProductTaxRepository taxes, IProductCatalog products, ILogger<SetProductTaxHandler> logger)
    : ICommandHandler<SetProductTaxCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetProductTaxCommand command, CancellationToken ct)
    {
        var product = await products.FindAsync(command.ProductId, ct);
        if (product is null)
            return ProductTaxErrors.ProductNotFound;
        if (!product.IsActive)
            return ProductTaxErrors.ProductNotActive;

        var tax = await taxes.GetForUpdateAsync(command.ProductId, ct);
        if (tax is null)
        {
            var created = ProductTax.Create(command.ProductId, command.TaxTreatment, command.TaxRate);
            if (created.IsFailure)
                return created.Error;
            taxes.Add(created.Value);
        }
        else
        {
            var set = tax.Set(command.TaxTreatment, command.TaxRate);
            if (set.IsFailure)
                return set.Error;
        }
        var saved = await taxes.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        TaxSet(logger, command.ProductId, command.TaxTreatment, command.TaxRate);
        return Done.Value;
    }

    [LoggerMessage(EventId = 7015, Level = LogLevel.Information,
        Message = "Tax classification of product {ProductId} set to {TaxTreatment} (own rate {TaxRate})")]
    private static partial void TaxSet(ILogger logger, Guid productId, SalesTaxTreatment taxTreatment, decimal? taxRate);
}
