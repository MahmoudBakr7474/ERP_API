using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Catalog.Application.Contracts.Products;
using WarehouseERP.Catalog.Application.Features.Products.CreateProduct;
using WarehouseERP.Catalog.Application.Features.Products.GetProduct;
using WarehouseERP.Catalog.Application.Features.Products.ListProducts;
using WarehouseERP.Catalog.Application.Features.Products.UpdateProduct;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Presentation.Contracts.Products;

namespace WarehouseERP.Catalog.Presentation;

// HTTP only: bind the request, map it field by field to a command or query (never bind an entity),
// call the handler pipeline and map the Result. No EF Core here.
internal static class ProductEndpoints
{
    public const string Route = "/api/catalog/products";

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Products");

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(CatalogPermissions.ProductsCreate)
            .WithName("CreateProduct")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(CatalogPermissions.ProductsRead)
            .WithName("GetProduct")
            .Produces<ProductDetailsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", ListAsync)
            .RequireAuthorization(CatalogPermissions.ProductsRead)
            .WithName("ListProducts")
            .Produces<PagedResult<ProductSummaryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(CatalogPermissions.ProductsUpdate)
            .WithName("UpdateProduct")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> CreateAsync(
        CreateProductCommand command, ICommandHandler<CreateProductCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> GetAsync(
        Guid id, IQueryHandler<GetProductQuery, ProductDetailsResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetProductQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListProductsQuery, PagedResult<ProductSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, string? search = null) =>
        (await handler.HandleAsync(new ListProductsQuery(page, pageSize, search), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateProductRequest request, ICommandHandler<UpdateProductCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new UpdateProductCommand(id, request.NameAr, request.NameEn, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));
}
