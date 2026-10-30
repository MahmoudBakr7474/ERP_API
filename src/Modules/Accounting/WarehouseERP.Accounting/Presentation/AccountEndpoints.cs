using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Application.Features.Accounts.ApplyDefaultChart;
using WarehouseERP.Accounting.Application.Features.Accounts.CreateAccount;
using WarehouseERP.Accounting.Application.Features.Accounts.DeleteAccount;
using WarehouseERP.Accounting.Application.Features.Accounts.GetAccount;
using WarehouseERP.Accounting.Application.Features.Accounts.GetAccountTree;
using WarehouseERP.Accounting.Application.Features.Accounts.ImportChartOfAccounts;
using WarehouseERP.Accounting.Application.Features.Accounts.ListAccounts;
using WarehouseERP.Accounting.Application.Features.Accounts.SetAccountActive;
using WarehouseERP.Accounting.Application.Features.Accounts.UpdateAccount;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Presentation.Contracts.Accounts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Presentation;

// The chart of accounts: reading needs accounting.accounts.read, changing accounting.accounts.manage; both company-wide.
// An empty chart is filled with the default chart or with an imported one.
internal static class AccountEndpoints
{
    public const string Route = "/api/accounting/accounts";

    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("Accounts");

        group.MapGet("/", ListAsync)
            .RequireAuthorization(AccountingPermissions.AccountsRead)
            .WithName("ListAccounts")
            .Produces<PagedResult<AccountSummaryResponse>>()
            .QueryProblems();

        group.MapGet("/tree", (IQueryHandler<GetAccountTreeQuery, IReadOnlyList<AccountSummaryResponse>> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetAccountTreeQuery(), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsRead)
            .WithName("GetAccountTree")
            .Produces<IReadOnlyList<AccountSummaryResponse>>()
            .QueryProblems();

        group.MapGet("/{id:guid}", (Guid id, IQueryHandler<GetAccountQuery, AccountResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new GetAccountQuery(id), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsRead)
            .WithName("GetAccount")
            .Produces<AccountResponse>()
            .QueryProblems();

        group.MapPost("/", (CreateAccountCommand command, ICommandHandler<CreateAccountCommand, Guid> handler, CancellationToken ct) =>
                EndpointConventions.CreatedAsync(handler.HandleAsync(command, ct), Route))
            .RequireAuthorization(AccountingPermissions.AccountsManage)
            .WithName("CreateAccount")
            .Produces<CreatedResponse>(StatusCodes.Status201Created)
            .CommandProblems();

        group.MapPut("/{id:guid}", (Guid id, UpdateAccountRequest request, ICommandHandler<UpdateAccountCommand, string> handler,
                    CancellationToken ct) =>
                EndpointConventions.UpdatedAsync(handler.HandleAsync(new UpdateAccountCommand(
                    id, request.NameAr, request.NameEn, request.Section, request.AllowsManualPosting, request.RowVersion), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsManage)
            .WithName("UpdateAccount")
            .Produces<UpdatedResponse>()
            .CommandProblems();

        group.MapDelete("/{id:guid}", (Guid id, ICommandHandler<DeleteAccountCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new DeleteAccountCommand(id), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsManage)
            .WithName("DeleteAccount")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        group.MapPost("/{id:guid}/activate", (Guid id, ICommandHandler<SetAccountActiveCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetAccountActiveCommand(id, IsActive: true), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsManage)
            .WithName("ActivateAccount")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        group.MapPost("/{id:guid}/deactivate", (Guid id, ICommandHandler<SetAccountActiveCommand, Done> handler, CancellationToken ct) =>
                EndpointConventions.NoContentAsync(handler.HandleAsync(new SetAccountActiveCommand(id, IsActive: false), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsManage)
            .WithName("DeactivateAccount")
            .Produces(StatusCodes.Status204NoContent)
            .CommandProblems();

        group.MapPost("/default-chart", (ICommandHandler<ApplyDefaultChartCommand, ChartImportedResponse> handler, CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(new ApplyDefaultChartCommand(), ct)))
            .RequireAuthorization(AccountingPermissions.AccountsManage)
            .WithName("ApplyDefaultChart")
            .Produces<ChartImportedResponse>()
            .CommandProblems();

        group.MapPost("/import", (ImportChartOfAccountsCommand command, ICommandHandler<ImportChartOfAccountsCommand, ChartImportedResponse> handler,
                    CancellationToken ct) =>
                EndpointConventions.OkAsync(handler.HandleAsync(command, ct)))
            .RequireAuthorization(AccountingPermissions.AccountsManage)
            .WithName("ImportChartOfAccounts")
            .Produces<ChartImportedResponse>()
            .CommandProblems();

        return app;
    }

    private static async Task<IResult> ListAsync(
        IQueryHandler<ListAccountsQuery, PagedResult<AccountSummaryResponse>> handler, CancellationToken ct,
        int page = 1, int? pageSize = null, string? search = null, AccountType? type = null, bool? isActive = null, bool? isPosting = null,
        Guid? parentId = null) =>
        (await handler.HandleAsync(new ListAccountsQuery(page, pageSize, search, type, isActive, isPosting, parentId), ct))
            .ToHttpResult(TypedResults.Ok);
}
