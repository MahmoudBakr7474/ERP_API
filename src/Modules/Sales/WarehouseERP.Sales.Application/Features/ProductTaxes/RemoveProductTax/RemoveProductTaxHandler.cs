using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.ProductTaxes;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.ProductTaxes.RemoveProductTax;

// Removing a classification deactivates it (its history stays): tills then refuse the product until it is classified again.
public sealed partial class RemoveProductTaxHandler(IProductTaxRepository taxes, ILogger<RemoveProductTaxHandler> logger)
    : ICommandHandler<RemoveProductTaxCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(RemoveProductTaxCommand command, CancellationToken ct)
    {
        var tax = await taxes.GetForUpdateAsync(command.ProductId, ct);
        if (tax is not { IsActive: true })
            return ProductTaxErrors.NotFound;
        tax.Deactivate();
        var saved = await taxes.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        TaxRemoved(logger, command.ProductId);
        return Done.Value;
    }

    [LoggerMessage(EventId = 7016, Level = LogLevel.Information, Message = "Tax classification of product {ProductId} removed")]
    private static partial void TaxRemoved(ILogger logger, Guid productId);
}
