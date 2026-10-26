using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;

// Optimistic concurrency: the product is loaded expecting the caller's row version, so a change
// made by someone else in between comes back as Common.ConcurrencyConflict (409).
// Returns the new row version for the caller's next update.
public sealed partial class UpdateProductHandler(IProductRepository products, ILogger<UpdateProductHandler> logger)
    : ICommandHandler<UpdateProductCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateProductCommand command, CancellationToken ct)
    {
        // The validator already checked the shape; a direct caller that skipped the pipeline gets the same error.
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return Error.Validation([new ValidationError(nameof(command.RowVersion), "RowVersionValidator", "Invalid row version.")]);
        var product = await products.GetForUpdateAsync(command.ProductId, expected, ct);
        if (product is null)
            return CatalogErrors.ProductNotFound;

        product.Rename(command.NameAr, command.NameEn);
        var saved = await products.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ProductRenamed(logger, product.Id);
        return Convert.ToBase64String(product.RowVersion);
    }

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Product {ProductId} renamed")]
    private static partial void ProductRenamed(ILogger logger, Guid productId);
}
