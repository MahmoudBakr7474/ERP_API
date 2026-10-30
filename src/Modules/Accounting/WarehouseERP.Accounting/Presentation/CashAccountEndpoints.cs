using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Contracts.CashAccounts;
using WarehouseERP.Accounting.Application.Features.CashAccounts.CreateCashAccount;
using WarehouseERP.Accounting.Application.Features.CashAccounts.GetCashAccount;
using WarehouseERP.Accounting.Application.Features.CashAccounts.ListCashAccounts;
using WarehouseERP.Accounting.Application.Features.CashAccounts.SetCashAccountActive;
using WarehouseERP.Accounting.Application.Features.CashAccounts.UpdateCashAccount;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Presentation.Contracts.CashAccounts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Presentation;

// Cashboxes and bank accounts with their balances: read with accounting.accounts.read, managed with
// accounting.treasury.manage; company-wide.
internal static class CashAccountEndpoints
{
    public const string Route = "/api/accounting/cash-accounts";

    public static IEndpointRouteBuilder MapCashAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("CashAccounts");

        group.MapGet("/", async (IQueryHandler<ListCashAccountsQuery, IReadOnlyList<CashAccountResponse>> handler, CancellationToken ct,
                    CashAccountKind? kind = null, Guid? branchId = null, bool? isActive = null, DateOnly? asOfDate = null) =>
                (await handler.HandleAsync(new ListCashAccountsQuery(kind, branchId, isActive, asOfDate), ct)).ToHttpResult(TypedResults.Ok))
            .RequireAuthorization(AccountingPermissions.AccountsRead)
            .WithName("ListCashAccounts")
            .Produces<IReadOnlyList<CashAccountResponse>>()
            .QueryProblems();

        group.MapGet("/{id:guid}", (Guid id, IQueryHandler<GetCashAccountQuery, CashAccountResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetCashAccountQuery(id), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsRead)
            .WithName("GetCashAccount")
            .Produces<CashAccountResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateCashAccountCommand command, ICommandHandler<CreateCashAccountCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(AccountingPermissions.TreasuryManage)
            .WithName("CreateCashAccount")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateCashAccountRequest request, ICommandHandler<UpdateCashAccountCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateCashAccountCommand(
                    id, request.Name, request.BankName, request.BankAccountNumber, request.AllowsOverdraft, request.RowVersion,
                    request.ReceivesPosCash), ct)))
            .RequireAuthorization(AccountingPermissions.TreasuryManage)
            .WithName("UpdateCashAccount")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetCashAccountActiveCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetCashAccountActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(AccountingPermissions.TreasuryManage)
            .WithName("ActivateCashAccount")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetCashAccountActiveCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetCashAccountActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(AccountingPermissions.TreasuryManage)
            .WithName("DeactivateCashAccount")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        return app;
    }
}
