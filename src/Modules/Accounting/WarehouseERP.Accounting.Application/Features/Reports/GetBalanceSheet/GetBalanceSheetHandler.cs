using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Contracts.Reports;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetBalanceSheet;

// Every posted line up to the date. Revenue and expenses not yet closed into retained earnings show as unclosed earnings
// inside equity, so the statement balances before and after a year-end close alike.
public sealed class GetBalanceSheetHandler(
    IAccountReadStore accounts, ILedgerReadStore ledger, AccountingReferences references, ICurrentUser currentUser)
    : IQueryHandler<GetBalanceSheetQuery, BalanceSheetResponse>
{
    private static readonly HashSet<StatementSection> Sections =
    [
        StatementSection.CurrentAssets, StatementSection.NonCurrentAssets, StatementSection.CurrentLiabilities,
        StatementSection.NonCurrentLiabilities, StatementSection.Equity,
    ];

    public async Task<Result<BalanceSheetResponse>> HandleAsync(GetBalanceSheetQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var chart = new ReportAccounts(await accounts.ListAllAsync(ct));
        var totals = (await ledger.TotalsAsync(null, query.AsOfDate, null, excludeYearEndClosing: false, ct)).ToDictionary(t => t.AccountId);
        var lines = StatementLines.Build(chart, Sections, totals, comparative: null);
        decimal Sum(StatementSection section) => lines.Where(l => l.Section == section).Sum(l => l.Amount);

        var unclosed = chart.All.Where(a => a.IsPosting && a.Type is AccountType.Revenue or AccountType.Expense)
            .Sum(a => totals.TryGetValue(a.Id, out var t) ? t.Credit - t.Debit : 0m);
        var currentAssets = Sum(StatementSection.CurrentAssets);
        var nonCurrentAssets = Sum(StatementSection.NonCurrentAssets);
        var currentLiabilities = Sum(StatementSection.CurrentLiabilities);
        var nonCurrentLiabilities = Sum(StatementSection.NonCurrentLiabilities);
        var equity = Sum(StatementSection.Equity);
        var totalAssets = currentAssets + nonCurrentAssets;
        var totalLiabilities = currentLiabilities + nonCurrentLiabilities;
        var totalEquity = equity + unclosed;
        return new BalanceSheetResponse(query.AsOfDate, company.Value.BaseCurrencyCode, lines, currentAssets, nonCurrentAssets, totalAssets,
            currentLiabilities, nonCurrentLiabilities, totalLiabilities, equity, unclosed, totalEquity, totalLiabilities + totalEquity,
            totalAssets == totalLiabilities + totalEquity);
    }
}
