using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Catalog.Application.Features.ProductUnits.AddProductUnit;
using WarehouseERP.Catalog.Application.Features.ProductUnits.SetProductUnitActive;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Presentation.Contracts.ProductUnits;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Presentation;

// HTTP only. A product's units are read with the product (GET /api/catalog/products/{id}); changing them is a product
// update (catalog.products.update).
internal static class ProductUnitEndpoints
{
    public const string Route = "/api/catalog/products/{productId:guid}/units";

    public static IEndpointRouteBuilder MapProductUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("ProductUnits");

        group.MapPost("/", AddAsync)
            .RequireAuthorization(CatalogPermissions.ProductsUpdate)
            .WithName("AddProductUnit")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{productUnitId:guid}/activate", (Guid productId, Guid productUnitId,
                    ICommandHandler<SetProductUnitActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetProductUnitActiveCommand(productId, productUnitId, IsActive: true), ct)))
            .RequireAuthorization(CatalogPermissions.ProductsUpdate)
            .WithName("ActivateProductUnit")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{productUnitId:guid}/deactivate", (Guid productId, Guid productUnitId,
                    ICommandHandler<SetProductUnitActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetProductUnitActiveCommand(productId, productUnitId, IsActive: false), ct)))
            .RequireAuthorization(CatalogPermissions.ProductsUpdate)
            .WithName("DeactivateProductUnit")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> AddAsync(
        Guid productId, AddProductUnitRequest request, ICommandHandler<AddProductUnitCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new AddProductUnitCommand(productId, request.UnitId, request.ConversionFactor), ct))
            .ToHttpResult(id => TypedResults.Created($"{ProductEndpoints.Route}/{productId}", new CreatedResponse(id)));

    private static async Task<IResult> NoContentAsync(Task<Result<Done>> pending) =>
        (await pending).ToHttpResult(_ => TypedResults.NoContent());
}
