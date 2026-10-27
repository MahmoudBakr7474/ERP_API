using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Catalog.Application.Features.ProductBarcodes.AddProductBarcode;
using WarehouseERP.Catalog.Application.Features.ProductBarcodes.SetProductBarcodeActive;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Presentation.Contracts.ProductBarcodes;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Presentation;

// HTTP only. Barcodes are read with the product (GET /api/catalog/products/{id}) and scanned through
// GET /api/catalog/products/lookup; changing them is a product update (catalog.products.update).
internal static class ProductBarcodeEndpoints
{
    public static IEndpointRouteBuilder MapProductBarcodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ProductEndpoints.Route + "/{productId:guid}").WithTags("ProductBarcodes");

        group.MapPost("/units/{productUnitId:guid}/barcodes", AddAsync)
            .RequireAuthorization(CatalogPermissions.ProductsUpdate)
            .WithName("AddProductBarcode")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/barcodes/{barcodeId:guid}/activate", (Guid productId, Guid barcodeId,
                    ICommandHandler<SetProductBarcodeActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetProductBarcodeActiveCommand(productId, barcodeId, IsActive: true), ct)))
            .RequireAuthorization(CatalogPermissions.ProductsUpdate)
            .WithName("ActivateProductBarcode")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/barcodes/{barcodeId:guid}/deactivate", (Guid productId, Guid barcodeId,
                    ICommandHandler<SetProductBarcodeActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetProductBarcodeActiveCommand(productId, barcodeId, IsActive: false), ct)))
            .RequireAuthorization(CatalogPermissions.ProductsUpdate)
            .WithName("DeactivateProductBarcode")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> AddAsync(
        Guid productId, Guid productUnitId, AddProductBarcodeRequest request,
        ICommandHandler<AddProductBarcodeCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new AddProductBarcodeCommand(productId, productUnitId, request.Barcode, request.BarcodeType), ct))
            .ToHttpResult(id => TypedResults.Created($"{ProductEndpoints.Route}/{productId}", new CreatedResponse(id)));

    private static async Task<IResult> NoContentAsync(Task<Result<Done>> pending) =>
        (await pending).ToHttpResult(_ => TypedResults.NoContent());
}
