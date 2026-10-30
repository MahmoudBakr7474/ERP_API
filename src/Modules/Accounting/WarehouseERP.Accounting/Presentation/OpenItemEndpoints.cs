using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Features.Allocations.AllocateOpenItems;
using WarehouseERP.Accounting.Application.Features.Allocations.AutoAllocate;
using WarehouseERP.Accounting.Application.Features.Allocations.RemoveAllocation;
using WarehouseERP.Accounting.Application.Features.OpenItems.ListOpenItems;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Presentation;

// Customers' and suppliers' open items (read with accounting.reports.read) and their allocations: allocating, removing an
// allocation and matching automatically need accounting.receivables.manage for customers and accounting.payables.manage
// for suppliers, which the handlers check (the party type is in the items).
internal static class OpenItemEndpoints
{
    public const string ItemsRoute = "/api/accounting/open-items";
    public const string AllocationsRoute = "/api/accounting/allocations";

    public static IEndpointRouteBuilder MapOpenItemEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup(ItemsRoute).WithTags("OpenItems")
            .MapGet("/", ListAsync)
            .RequireAuthorization(AccountingPermissions.ReportsRead)
            .WithName("ListOpenItems")
            .Produces<PagedResult<OpenItemResponse>>()
            .QueryProblems();

        var allocations = app.MapGroup(AllocationsRoute).WithTags("OpenItems");

        allocations.MapPost("/", (AllocateOpenItemsCommand command, ICommandHandler<AllocateOpenItemsCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), AllocationsRoute))
            .RequireAuthorization()
            .WithName("AllocateOpenItems")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        allocations.MapPost("/{id:guid}/remove", (Guid id, ICommandHandler<RemoveAllocationCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new RemoveAllocationCommand(id), ct)))
            .RequireAuthorization()
            .WithName("RemoveAllocation")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        allocations.MapPost("/auto", (AutoAllocateCommand command, ICommandHandler<AutoAllocateCommand, AutoAllocatedResponse> handler,
                    CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(command, ct)))
            .RequireAuthorization()
            .WithName("AutoAllocate")
            .Produces<AutoAllocatedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListOpenItemsQuery, PagedResult<OpenItemResponse>> handler, CancellationToken ct, int page = 1, int? pageSize = null,
        PartyType? partyType = null, Guid? partyId = null, Guid? accountId = null, bool openOnly = true) =>
        (await handler.HandleAsync(new ListOpenItemsQuery(page, pageSize, partyType, partyId, accountId, openOnly), ct)).ToHttpResult(TypedResults.Ok);
}
