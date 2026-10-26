using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Catalog.Application.Abstractions.Interfaces.Products;
using WarehouseERP.Catalog.Application.Errors;
using WarehouseERP.Catalog.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Application.Features.Products.CreateProduct;

// Owns the transaction: one save point (IProductRepository.AddAsync) for the product and its base
// unit row. The pre-checks give clear errors; the database still guards the SKU if two requests race.
public sealed partial class CreateProductHandler(IProductRepository products, ILogger<CreateProductHandler> logger)
    : ICommandHandler<CreateProductCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateProductCommand command, CancellationToken ct)
    {
        if (!await products.IsUnitActiveAsync(command.BaseUnitId, ct))
            return UnitErrors.NotActive;
        if (command.CategoryId is { } categoryId && !await products.IsCategoryActiveAsync(categoryId, ct))
            return CategoryErrors.NotActive;
        if (await products.SkuExistsAsync(command.Sku.Trim(), ct))
            return ProductErrors.SkuTaken;

        var created = Product.Create(
            command.Sku, command.NameAr, command.NameEn, command.BaseUnitId, command.CategoryId, command.Type, command.TrackingMode);
        if (created.IsFailure)
            return created.Error;

        var saved = await products.AddAsync(created.Value, ct);
        if (saved.IsFailure)
            return saved.Error;

        ProductCreated(logger, created.Value.Id, created.Value.SKU);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Product {ProductId} ({Sku}) created")]
    private static partial void ProductCreated(ILogger logger, Guid productId, string sku);
}
