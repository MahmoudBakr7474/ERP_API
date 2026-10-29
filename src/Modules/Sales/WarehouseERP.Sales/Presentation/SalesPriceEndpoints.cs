using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Sales.Application.Contracts.Prices;
using WarehouseERP.Sales.Application.Features.Prices.ListSalesPrices;
using WarehouseERP.Sales.Application.Features.Prices.RemoveSalesPrice;
using WarehouseERP.Sales.Application.Features.Prices.SetSalesPrice;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Presentation.Contracts.Prices;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Presentation;

// The company-wide price list: reading needs sales.documents.read (order screens price their lines from it), setting
// and removing prices sales.prices.manage.
internal static class SalesPriceEndpoints
{
    public const string Route = "/api/sales/prices";

    public static IEndpointRouteBuilder MapSalesPriceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SalesPrices");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("ListSalesPrices")
            .Produces<PagedResult<SalesPriceResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{productUnitId:guid}", (Guid productUnitId, SetSalesPriceRequest request, ICommandHandler<SetSalesPriceCommand, Done> handler,
                    CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetSalesPriceCommand(productUnitId, request.UnitPrice), ct)))
            .RequireAuthorization(SalesPermissions.PricesManage)
            .WithName("SetSalesPrice")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        group.MapDelete("/{productUnitId:guid}", (Guid productUnitId, ICommandHandler<RemoveSalesPriceCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new RemoveSalesPriceCommand(productUnitId), ct)))
            .RequireAuthorization(SalesPermissions.PricesManage)
            .WithName("RemoveSalesPrice")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListSalesPricesQuery, PagedResult<SalesPriceResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? productId = null, Guid? productUnitId = null) =>
        (await handler.HandleAsync(new ListSalesPricesQuery(page, pageSize, productId, productUnitId), ct)).ToHttpResult(TypedResults.Ok);
}
