using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WarehouseERP.Accounting.Application.Features.Reports.GetAccountStatement;
using WarehouseERP.Accounting.Application.Features.Reports.GetAging;
using WarehouseERP.Accounting.Application.Features.Reports.GetBalanceSheet;
using WarehouseERP.Accounting.Application.Features.Reports.GetIncomeStatement;
using WarehouseERP.Accounting.Application.Features.Reports.GetPartyStatement;
using WarehouseERP.Accounting.Application.Features.Reports.GetSubledgerReconciliation;
using WarehouseERP.Accounting.Application.Features.Reports.GetTrialBalance;
using WarehouseERP.Accounting.Application.Features.Reports.ListGeneralLedger;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Infrastructure.Http;

namespace WarehouseERP.Accounting.Presentation;

// Financial reports from posted journals only, for company-wide users with accounting.reports.read. Dates are posting
// dates; a branch filter narrows to that branch's lines (the balance sheet is company-level only).
internal static class FinancialReportEndpoints
{
    public const string Route = "/api/accounting/reports";

    public static IEndpointRouteBuilder MapFinancialReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(Route).WithTags("FinancialReports").RequireAuthorization(AccountingPermissions.ReportsRead);

        group.MapGet("/trial-balance", async (IQueryHandler<GetTrialBalanceQuery, TrialBalanceResponse> handler, CancellationToken ct,
                    DateOnly fromDate, DateOnly toDate, Guid? branchId = null, int? level = null, bool includeZero = false) =>
                (await handler.HandleAsync(new GetTrialBalanceQuery(fromDate, toDate, branchId, level, includeZero), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("GetTrialBalance")
            .Produces<TrialBalanceResponse>()
            .QueryProblems();

        group.MapGet("/account-statement", async (IQueryHandler<GetAccountStatementQuery, AccountStatementResponse> handler,
                    CancellationToken ct, Guid accountId, DateOnly fromDate, DateOnly toDate, Guid? branchId = null) =>
                (await handler.HandleAsync(new GetAccountStatementQuery(accountId, fromDate, toDate, branchId), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("GetAccountStatement")
            .Produces<AccountStatementResponse>()
            .QueryProblems();

        group.MapGet("/general-ledger", async (IQueryHandler<ListGeneralLedgerQuery, PagedResult<GeneralLedgerLineResponse>> handler,
                    CancellationToken ct, DateOnly fromDate, DateOnly toDate, int page = 1, int? pageSize = null, Guid? accountId = null,
                    Guid? branchId = null, JournalType? journalType = null) =>
                (await handler.HandleAsync(new ListGeneralLedgerQuery(page, pageSize, fromDate, toDate, accountId, branchId, journalType), ct))
                    .ToHttpResult(TypedResults.Ok))
            .WithName("ListGeneralLedger")
            .Produces<PagedResult<GeneralLedgerLineResponse>>()
            .QueryProblems();

        group.MapGet("/balance-sheet", async (IQueryHandler<GetBalanceSheetQuery, BalanceSheetResponse> handler, CancellationToken ct,
                    DateOnly asOfDate) =>
                (await handler.HandleAsync(new GetBalanceSheetQuery(asOfDate), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("GetBalanceSheet")
            .Produces<BalanceSheetResponse>()
            .QueryProblems();

        group.MapGet("/income-statement", async (IQueryHandler<GetIncomeStatementQuery, IncomeStatementResponse> handler, CancellationToken ct,
                    DateOnly fromDate, DateOnly toDate, Guid? branchId = null, DateOnly? compareFromDate = null, DateOnly? compareToDate = null) =>
                (await handler.HandleAsync(new GetIncomeStatementQuery(fromDate, toDate, branchId, compareFromDate, compareToDate), ct))
                    .ToHttpResult(TypedResults.Ok))
            .WithName("GetIncomeStatement")
            .Produces<IncomeStatementResponse>()
            .QueryProblems();

        group.MapGet("/aging", async (IQueryHandler<GetAgingQuery, AgingResponse> handler, CancellationToken ct, PartyType partyType,
                    DateOnly asOfDate, Guid? branchId = null) =>
                (await handler.HandleAsync(new GetAgingQuery(partyType, asOfDate, branchId), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("GetAging")
            .Produces<AgingResponse>()
            .QueryProblems();

        group.MapGet("/party-statement", async (IQueryHandler<GetPartyStatementQuery, PartyStatementResponse> handler, CancellationToken ct,
                    PartyType partyType, Guid partyId, DateOnly fromDate, DateOnly toDate) =>
                (await handler.HandleAsync(new GetPartyStatementQuery(partyType, partyId, fromDate, toDate), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("GetPartyStatement")
            .Produces<PartyStatementResponse>()
            .QueryProblems();

        group.MapGet("/subledger-reconciliation", async (IQueryHandler<GetSubledgerReconciliationQuery, SubledgerReconciliationResponse> handler,
                    CancellationToken ct, DateOnly asOfDate) =>
                (await handler.HandleAsync(new GetSubledgerReconciliationQuery(asOfDate), ct)).ToHttpResult(TypedResults.Ok))
            .WithName("GetSubledgerReconciliation")
            .Produces<SubledgerReconciliationResponse>()
            .QueryProblems();

        return app;
    }
}
