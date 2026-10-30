using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Contracts.SourcePostings;
using WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;
using WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceSyncStatus;
using WarehouseERP.Accounting.Application.Features.SourcePostings.ListSourcePostings;
using WarehouseERP.Accounting.Application.Features.SourcePostings.RetrySourcePosting;
using WarehouseERP.Accounting.Application.Features.SourcePostings.RunSourceSync;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;

namespace WarehouseERP.Accounting.Presentation;

// The automatic journaling of operational documents: its exception queue, status, on-demand run and retry, and the
// source-to-ledger reconciliation.
internal static class SourcePostingEndpoints
{
    public const string Route = "/api/accounting/source-postings";

    public static IEndpointRouteBuilder MapSourcePostingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("SourcePostings");

        group.MapGet("/", async (IQueryHandler<ListSourcePostingsQuery, PagedResult<SourcePostingResponse>> handler, CancellationToken ct,
                    int page = 1, int? pageSize = null, SourcePostingStatus? status = null, SourceDocumentType? sourceType = null,
                    DateOnly? fromDate = null, DateOnly? toDate = null, bool varianceOnly = false) =>
                (await handler.HandleAsync(new ListSourcePostingsQuery(page, pageSize, status, sourceType, fromDate, toDate, varianceOnly), ct))
                .ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(AccountingPermissions.JournalsRead)
            .WithName("ListSourcePostings")
            .Produces<PagedResult<SourcePostingResponse>>()
            .QueryProblems();

        group.MapGet("/status", (IQueryHandler<GetSourceSyncStatusQuery, SourceSyncStatusResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetSourceSyncStatusQuery(), ct)))
            .RequireAuthorization(AccountingPermissions.JournalsRead)
            .WithName("GetSourceSyncStatus")
            .Produces<SourceSyncStatusResponse>()
            .QueryProblems();

        group.MapGet("/reconciliation", (IQueryHandler<GetSourceReconciliationQuery, SourceReconciliationResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetSourceReconciliationQuery(), ct)))
            .RequireAuthorization(AccountingPermissions.ReportsRead)
            .WithName("GetSourceReconciliation")
            .Produces<SourceReconciliationResponse>()
            .QueryProblems();

        group.MapPost("/run", (RunSourceSyncCommand command, ICommandHandler<RunSourceSyncCommand, SourceSyncResponse> handler,
                    CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(command, ct)))
            .RequireAuthorization(AccountingPermissions.IntegrationManage)
            .WithName("RunSourceSync")
            .Produces<SourceSyncResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/retry", (Guid id, ICommandHandler<RetrySourcePostingCommand, SourcePostingResponse> handler,
                    CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new RetrySourcePostingCommand(id), ct)))
            .RequireAuthorization(AccountingPermissions.IntegrationManage)
            .WithName("RetrySourcePosting")
            .Produces<SourcePostingResponse>()
            .CommandProblems();

        return app;
    }
}
