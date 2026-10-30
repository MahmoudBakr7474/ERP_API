using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.Organization.Application.Contracts.Reports;
using WarehouseERP.Organization.Application.Features.Reports.GetOrganizationStructure;
using WarehouseERP.Organization.Application.Features.Reports.GetOrganizationSummary;
using WarehouseERP.Organization.Application.Features.Reports.ListOrganizationDataIssues;
using WarehouseERP.Organization.Application.Features.WarehouseLocations.ListLocationDirectory;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Presentation;

// Master-data reports over the whole organization (docs/modules/Organization/REPORTS_AND_IMPORTS.md). They show branches
// and warehouses together, so they need both read permissions; the handlers keep them within the caller's warehouses.
// The location directory lists locations across warehouses and needs only the warehouse read permission, like a
// warehouse's own location list.
internal static class OrganizationReportEndpoints
{
    public const string Route = "/api/organization/reports";
    public const string LocationsRoute = "/api/organization/locations";

    public static IEndpointRouteBuilder MapOrganizationReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("OrganizationReports")
            .RequireAuthorization(OrganizationPermissions.BranchesRead, OrganizationPermissions.WarehousesRead);

        group.MapGet("/summary", async (IQueryHandler<GetOrganizationSummaryQuery, OrganizationSummaryResponse> handler, CancellationToken ct) =>
                (await handler.HandleAsync(new GetOrganizationSummaryQuery(), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("GetOrganizationSummary")
            .Produces<OrganizationSummaryResponse>();

        group.MapGet("/structure", async (
                IQueryHandler<GetOrganizationStructureQuery, IReadOnlyList<StructureBranchResponse>> handler, CancellationToken ct,
                bool? isActive) =>
                (await handler.HandleAsync(new GetOrganizationStructureQuery(isActive), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("GetOrganizationStructure")
            .Produces<IReadOnlyList<StructureBranchResponse>>();

        group.MapGet("/data-quality", async (
                IQueryHandler<ListOrganizationDataIssuesQuery, OrganizationDataQualityResponse> handler, CancellationToken ct,
                OrganizationDataIssue? issue, int page = 1, int? pageSize = null) =>
                (await handler.HandleAsync(new ListOrganizationDataIssuesQuery(issue, page, pageSize), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("ListOrganizationDataIssues")
            .Produces<OrganizationDataQualityResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        app.MapGet(LocationsRoute, ListLocationsAsync)
            .WithTags("WarehouseLocations")
            .RequireAuthorization(OrganizationPermissions.WarehousesRead)
            .WithName("ListLocationDirectory")
            .Produces<PagedResult<LocationDirectoryResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden);
        return app;
    }

    private static async Task<IResult> ListLocationsAsync(
        IQueryHandler<ListLocationDirectoryQuery, PagedResult<LocationDirectoryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, Guid? branchId = null, Guid? warehouseId = null, WarehouseType? warehouseType = null,
        string? search = null, bool? isActive = null, OrganizationSort? sort = null, bool descending = false) =>
        (await handler.HandleAsync(
                new ListLocationDirectoryQuery(page, pageSize, branchId, warehouseId, warehouseType, search, isActive, sort, descending), ct))
            .ToHttpResult(TypedResults.Ok);
}
