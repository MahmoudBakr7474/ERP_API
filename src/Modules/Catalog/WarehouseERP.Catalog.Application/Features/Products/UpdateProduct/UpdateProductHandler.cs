using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;

// Optimistic concurrency: the product is loaded expecting the caller's row version, so a change
// made by someone else in between comes back as Common.ConcurrencyConflict (409).
// SKU, type, tracking and base unit are fixed; a newly chosen category must be active.
// Returns the new row version for the caller's next update.
public sealed partial class UpdateProductHandler(IProductRepository products, ILogger<UpdateProductHandler> logger)
    : ICommandHandler<UpdateProductCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateProductCommand command, CancellationToken ct)
    {
        // The validator already checked the shape; a direct caller that skipped the pipeline gets the same error.
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var product = await products.GetForUpdateAsync(command.ProductId, expected, ct);
        if (product is null)
            return ProductErrors.NotFound;
        if (command.CategoryId is { } categoryId && categoryId != product.CategoryId
            && !await products.IsCategoryActiveAsync(categoryId, ct))
            return CategoryErrors.NotActive;

        product.Update(command.NameAr, command.NameEn, command.CategoryId);
        var saved = await products.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ProductUpdated(logger, product.Id);
        return Convert.ToBase64String(product.RowVersion);
    }

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "Product {ProductId} updated")]
    private static partial void ProductUpdated(ILogger logger, Guid productId);
}
