using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Application.Features.Journals.ApproveJournal;
using WarehouseERP.Accounting.Application.Features.Journals.CancelJournal;
using WarehouseERP.Accounting.Application.Features.Journals.CreateJournal;
using WarehouseERP.Accounting.Application.Features.Journals.CreateOpeningJournal;
using WarehouseERP.Accounting.Application.Features.Journals.GetJournal;
using WarehouseERP.Accounting.Application.Features.Journals.ListJournals;
using WarehouseERP.Accounting.Application.Features.Journals.PostJournal;
using WarehouseERP.Accounting.Application.Features.Journals.RejectJournal;
using WarehouseERP.Accounting.Application.Features.Journals.ReverseJournal;
using WarehouseERP.Accounting.Application.Features.Journals.SubmitJournal;
using WarehouseERP.Accounting.Application.Features.Journals.UpdateJournal;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Presentation.Contracts.Common;
using WarehouseERP.Accounting.Presentation.Contracts.Journals;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;

namespace WarehouseERP.Accounting.Presentation;

// Journals: reading needs accounting.journals.read. Drafting manual journals and reversals needs accounting.journals.manage,
// opening balances accounting.opening.manage; editing, submitting, posting and cancelling an existing journal need the one
// of its type, which the handler checks (the type is in the journal). Approving and rejecting need
// accounting.journals.approve — whoever wrote the journal may approve it. Post and approve carry an Idempotency-Key.
internal static class JournalEndpoints
{
    public const string Route = "/api/accounting/journals";

    public static IEndpointRouteBuilder MapJournalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Journals");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(AccountingPermissions.JournalsRead)
            .WithName("ListJournals")
            .Produces<PagedResult<JournalSummaryResponse>>()
            .QueryProblems();

        group.MapGet("/{id:guid}", (Guid id, IQueryHandler<GetJournalQuery, JournalResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetJournalQuery(id), ct)))
            .RequireAuthorization(AccountingPermissions.JournalsRead)
            .WithName("GetJournal")
            .Produces<JournalResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateJournalCommand command, ICommandHandler<CreateJournalCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(AccountingPermissions.JournalsManage)
            .WithName("CreateJournal")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPost("/opening", (CreateOpeningJournalCommand command, ICommandHandler<CreateOpeningJournalCommand, Guid> handler,
                    CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(AccountingPermissions.OpeningBalancesManage)
            .WithName("CreateOpeningJournal")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateJournalRequest request, ICommandHandler<UpdateJournalCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateJournalCommand(
                    id, request.PostingDate, request.Description, request.Reference, request.Lines, request.RowVersion), ct)))
            .RequireAuthorization()
            .WithName("UpdateJournal")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/submit", (Guid id, AccountingActionRequest request, ICommandHandler<SubmitJournalCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new SubmitJournalCommand(id, request.RowVersion), ct)))
            .RequireAuthorization()
            .WithName("SubmitJournal")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/post", PostAsync)
            .RequireAuthorization()
            .WithName("PostJournal")
            .Produces<JournalPostedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/approve", ApproveAsync)
            .RequireAuthorization(AccountingPermissions.JournalsApprove)
            .WithName("ApproveJournal")
            .Produces<JournalPostedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reject", (Guid id, AccountingReasonRequest request, ICommandHandler<RejectJournalCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new RejectJournalCommand(id, request.Reason ?? "", request.RowVersion), ct)))
            .RequireAuthorization(AccountingPermissions.JournalsApprove)
            .WithName("RejectJournal")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, AccountingActionRequest request, ICommandHandler<CancelJournalCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelJournalCommand(id, request.RowVersion), ct)))
            .RequireAuthorization()
            .WithName("CancelJournal")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/reverse", (Guid id, ReverseJournalRequest request, ICommandHandler<ReverseJournalCommand, Guid> handler,
                    CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(new ReverseJournalCommand(
                    id, request.PostingDate, request.Description, request.Reference), ct), Route))
            .RequireAuthorization(AccountingPermissions.JournalsManage)
            .WithName("ReverseJournal")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListJournalsQuery, PagedResult<JournalSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, JournalStatus? status = null, JournalType? type = null, DateOnly? fromDate = null,
        DateOnly? toDate = null, string? search = null) =>
        (await handler.HandleAsync(new ListJournalsQuery(page, pageSize, status, type, fromDate, toDate, search), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> PostAsync(
        Guid id, AccountingActionRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<PostJournalCommand, JournalPostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new PostJournalCommand(id, request.RowVersion, idempotencyKey ?? Guid.Empty), ct)).ToHttpResult(TypedResults.Ok);

    private static async Task<IResult> ApproveAsync(
        Guid id, AccountingActionRequest request, [FromHeader(Name = EndpointConventions.IdempotencyHeader)] Guid? idempotencyKey,
        ICommandHandler<ApproveJournalCommand, JournalPostedResponse> handler, CancellationToken ct) =>
        (await handler.HandleAsync(new ApproveJournalCommand(id, request.RowVersion, idempotencyKey ?? Guid.Empty), ct)).ToHttpResult(TypedResults.Ok);
}
