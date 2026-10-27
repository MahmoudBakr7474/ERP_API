using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Catalog.Application.Contracts.Categories;
using WarehouseERP.Catalog.Application.Features.Categories.CreateCategory;
using WarehouseERP.Catalog.Application.Features.Categories.GetCategory;
using WarehouseERP.Catalog.Application.Features.Categories.ListCategories;
using WarehouseERP.Catalog.Application.Features.Categories.SetCategoryActive;
using WarehouseERP.Catalog.Application.Features.Categories.UpdateCategory;
using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Catalog.Presentation.Contracts.Categories;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Presentation;

// HTTP only: bind the request, map it to a command or query, call the handler pipeline and map the Result.
// Anyone who reads products reads categories; changes need catalog.categories.manage.
internal static class CategoryEndpoints
{
    public const string Route = "/api/catalog/categories";

    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Categories");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(CatalogPermissions.ProductsRead)
            .WithName("ListCategories")
            .Produces<IReadOnlyList<CategoryResponse>>();

        group.MapGet("/{id:guid}", GetAsync)
            .RequireAuthorization(CatalogPermissions.ProductsRead)
            .WithName("GetCategory")
            .Produces<CategoryDetailsResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .RequireAuthorization(CatalogPermissions.CategoriesManage)
            .WithName("CreateCategory")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}", UpdateAsync)
            .RequireAuthorization(CatalogPermissions.CategoriesManage)
            .WithName("UpdateCategory")
            .Produces<UpdatedResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetCategoryActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetCategoryActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(CatalogPermissions.CategoriesManage)
            .WithName("ActivateCategory")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetCategoryActiveCommand, Done> handler, CancellationToken ct) =>
                NoContentAsync(handler.HandleAsync(new SetCategoryActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(CatalogPermissions.CategoriesManage)
            .WithName("DeactivateCategory")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListCategoriesQuery, IReadOnlyList<CategoryResponse>> handler, CancellationToken ct, bool? isActive = null) =>
        (await handler.HandleAsync(new ListCategoriesQuery(isActive), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> GetAsync(
        Guid id, IQueryHandler<GetCategoryQuery, CategoryDetailsResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new GetCategoryQuery(id), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> CreateAsync(
        CreateCategoryCommand command, ICommandHandler<CreateCategoryCommand, Guid> handler, CancellationToken ct) =>
        (await handler.HandleAsync(command, ct))
            .ToHttpResult(id => TypedResults.Created($"{Route}/{id}", new CreatedResponse(id)));

    private static async Task<IResult> UpdateAsync(
        Guid id, UpdateCategoryRequest request, ICommandHandler<UpdateCategoryCommand, string> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new UpdateCategoryCommand(id, request.NameAr, request.NameEn, request.ParentId, request.RowVersion), ct))
            .ToHttpResult(rowVersion => TypedResults.Ok(new UpdatedResponse(rowVersion)));

    private static async Task<IResult> NoContentAsync(Task<Result<Done>> pending) =>
        (await pending).ToHttpResult(_ => TypedResults.NoContent());
}
