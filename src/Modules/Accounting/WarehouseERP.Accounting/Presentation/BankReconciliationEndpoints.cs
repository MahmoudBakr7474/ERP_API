using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Contracts.BankReconciliations;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.CancelBankReconciliation;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.CompleteBankReconciliation;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.GetBankReconciliation;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.ListBankReconciliations;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.StartBankReconciliation;
using WarehouseERP.Accounting.Application.Features.BankReconciliations.TickBankReconciliationLines;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Presentation.Contracts.BankReconciliations;
using WarehouseERP.Accounting.Presentation.Contracts.Common;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;

namespace WarehouseERP.Accounting.Presentation;

// Manual bank reconciliation (V1): start from a statement's date and closing balance, tick the cleared ledger lines,
// complete when nothing differs. accounting.treasury.manage, company-wide.
internal static class BankReconciliationEndpoints
{
    public const string Route = "/api/accounting/bank-reconciliations";

    public static IEndpointRouteBuilder MapBankReconciliationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("BankReconciliations").RequireAuthorization(AccountingPermissions.TreasuryManage);

        group.MapGet("/", async (IQueryHandler<ListBankReconciliationsQuery, IReadOnlyList<BankReconciliationSummaryResponse>> handler,
                    CancellationToken ct, Guid? cashAccountId = null) =>
                (await handler.HandleAsync(new ListBankReconciliationsQuery(cashAccountId), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("ListBankReconciliations")
            .Produces<IReadOnlyList<BankReconciliationSummaryResponse>>()
            .QueryProblems();

        group.MapGet("/{id:guid}", (Guid id, IQueryHandler<GetBankReconciliationQuery, BankReconciliationResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetBankReconciliationQuery(id), ct)))
            .WithName("GetBankReconciliation")
            .Produces<BankReconciliationResponse>()
            .QueryProblems();

        group.MapPost("/", (StartBankReconciliationCommand command, ICommandHandler<StartBankReconciliationCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .WithName("StartBankReconciliation")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}/lines", (Guid id, TickBankReconciliationLinesRequest request,
                    ICommandHandler<TickBankReconciliationLinesCommand, string> handler, CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new TickBankReconciliationLinesCommand(id, request.JournalLineIds, request.RowVersion), ct)))
            .WithName("TickBankReconciliationLines")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/complete", (Guid id, AccountingActionRequest request, ICommandHandler<CompleteBankReconciliationCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CompleteBankReconciliationCommand(id, request.RowVersion), ct)))
            .WithName("CompleteBankReconciliation")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/cancel", (Guid id, AccountingActionRequest request, ICommandHandler<CancelBankReconciliationCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new CancelBankReconciliationCommand(id, request.RowVersion), ct)))
            .WithName("CancelBankReconciliation")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        return app;
    }
}
