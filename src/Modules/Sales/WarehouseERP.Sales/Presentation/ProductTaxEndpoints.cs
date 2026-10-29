using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Sales.Application.Contracts.ProductTaxes;
using WarehouseERP.Sales.Application.Features.ProductTaxes.ListProductTaxes;
using WarehouseERP.Sales.Application.Features.ProductTaxes.RemoveProductTax;
using WarehouseERP.Sales.Application.Features.ProductTaxes.SetProductTax;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Presentation.Contracts.ProductTaxes;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Presentation;

// Products' tax classifications, company-wide like the price list: reading needs sales.documents.read, classifying and
// removing sales.prices.manage (whoever maintains selling prices). A till refuses a product without one.
internal static class ProductTaxEndpoints
{
    public const string Route = "/api/sales/product-taxes";

    public static IEndpointRouteBuilder MapProductTaxEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("ProductTaxes");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(SalesPermissions.DocumentsRead)
            .WithName("ListProductTaxes")
            .Produces<PagedResult<ProductTaxResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{productId:guid}", (Guid productId, SetProductTaxRequest request, ICommandHandler<SetProductTaxCommand, Done> handler,
                    CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetProductTaxCommand(productId, request.TaxTreatment, request.TaxRate), ct)))
            .RequireAuthorization(SalesPermissions.PricesManage)
            .WithName("SetProductTax")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        group.MapDelete("/{productId:guid}", (Guid productId, ICommandHandler<RemoveProductTaxCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new RemoveProductTaxCommand(productId), ct)))
            .RequireAuthorization(SalesPermissions.PricesManage)
            .WithName("RemoveProductTax")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListProductTaxesQuery, PagedResult<ProductTaxResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? productId = null, SalesTaxTreatment? taxTreatment = null) =>
        (await handler.HandleAsync(new ListProductTaxesQuery(page, pageSize, productId, taxTreatment), ct)).ToHttpResult(TypedResults.Ok);
}
