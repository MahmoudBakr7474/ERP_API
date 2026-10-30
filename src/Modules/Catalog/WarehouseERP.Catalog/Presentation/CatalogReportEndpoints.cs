using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Catalog.Application.Contracts.Reports;
using WarehouseERP.Catalog.Application.Features.Reports.GetCatalogSummary;
using WarehouseERP.Catalog.Application.Features.Reports.ListCategoryProductCounts;
using WarehouseERP.Catalog.Application.Features.Reports.ListDataQualityIssues;
using WarehouseERP.Catalog.Application.Features.Reports.ListUnitConversions;
using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Presentation;

// The catalog reports as JSON for the report screens. Their downloads (CatalogExportEndpoints) run the same read
// methods with the same filters, so a screen and its file always agree.
internal static class CatalogReportEndpoints
{
    public const string Route = "/api/catalog/reports";

    public static IEndpointRouteBuilder MapCatalogReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("CatalogReports").RequireAuthorization(CatalogPermissions.ProductsRead);

        group.MapGet("/summary", async (IQueryHandler<GetCatalogSummaryQuery, CatalogSummaryResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetCatalogSummaryQuery(), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("GetCatalogSummary")
            .Produces<CatalogSummaryResponse>();

        group.MapGet("/categories", async (
                    IQueryHandler<ListCategoryProductCountsQuery, IReadOnlyList<CategoryProductCountResponse>> handler,
                    CancellationToken ct, bool? isActive = null) =>
                (await handler.HandleAsync(new ListCategoryProductCountsQuery(isActive), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("ListCategoryProductCounts")
            .Produces<IReadOnlyList<CategoryProductCountResponse>>();

        group.MapGet("/data-quality", async (
                    IQueryHandler<ListDataQualityIssuesQuery, DataQualityReportResponse> handler, CancellationToken ct,
                    int page = 1, int? pageSize = null, CatalogDataIssue? issue = null, string? search = null, bool? isActive = null) =>
                (await handler.HandleAsync(new ListDataQualityIssuesQuery(page, pageSize, issue, search, isActive), ct))
                .ToHttpResult(TypedResults.Ok))
            .WithName("ListCatalogDataQualityIssues")
            .Produces<DataQualityReportResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/unit-conversions", async (
                    IQueryHandler<ListUnitConversionsQuery, PagedResult<UnitConversionResponse>> handler, CancellationToken ct,
                    int page = 1, int? pageSize = null, string? search = null, Guid? categoryId = null, ProductType? type = null,
                    bool? isActive = null, Guid? unitId = null) =>
                (await handler.HandleAsync(new ListUnitConversionsQuery(page, pageSize, search, categoryId, type, isActive, unitId), ct))
                .ToHttpResult(TypedResults.Ok))
            .WithName("ListUnitConversions")
            .Produces<PagedResult<UnitConversionResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }
}
