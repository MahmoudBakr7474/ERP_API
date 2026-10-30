using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Contracts.Reports;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetIncomeStatement;

// Posted revenue and expense lines over the range, year-end closing journals left out (they move results into retained
// earnings, they don't earn or spend anything), so a closed year still shows its results. Optionally one branch and a
// comparison period.
public sealed class GetIncomeStatementHandler(
    IAccountReadStore accounts, ILedgerReadStore ledger, AccountingReferences references, ICurrentUser currentUser)
    : IQueryHandler<GetIncomeStatementQuery, IncomeStatementResponse>
{
    private static readonly HashSet<StatementSection> Sections =
    [
        StatementSection.Revenue, StatementSection.CostOfSales, StatementSection.OperatingExpenses, StatementSection.OtherIncome,
        StatementSection.OtherExpenses, StatementSection.IncomeTax,
    ];

    public async Task<Result<IncomeStatementResponse>> HandleAsync(GetIncomeStatementQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var chart = new ReportAccounts(await accounts.ListAllAsync(ct));
        var totals = (await ledger.TotalsAsync(query.FromDate, query.ToDate, query.BranchId, excludeYearEndClosing: true, ct))
            .ToDictionary(t => t.AccountId);
        Dictionary<Guid, AccountTotals>? compared = null;
        if (query.CompareFromDate is { } compareFrom && query.CompareToDate is { } compareTo)
            compared = (await ledger.TotalsAsync(compareFrom, compareTo, query.BranchId, excludeYearEndClosing: true, ct))
                .ToDictionary(t => t.AccountId);

        var lines = StatementLines.Build(chart, Sections, totals, compared);
        var comparativeTotals = compared is null ? null : Totals(lines.Select(l => (l.Section, l.ComparativeAmount ?? 0m)));
        return new IncomeStatementResponse(query.FromDate, query.ToDate, query.BranchId, query.CompareFromDate, query.CompareToDate,
            company.Value.BaseCurrencyCode, lines, Totals(lines.Select(l => (l.Section, l.Amount))), comparativeTotals);
    }

    private static IncomeStatementTotalsResponse Totals(IEnumerable<(StatementSection Section, decimal Amount)> amounts)
    {
        var bySection = amounts.GroupBy(a => a.Section).ToDictionary(g => g.Key, g => g.Sum(a => a.Amount));
        decimal Of(StatementSection section) => bySection.GetValueOrDefault(section);
        var revenue = Of(StatementSection.Revenue);
        var costOfSales = Of(StatementSection.CostOfSales);
        var grossProfit = revenue - costOfSales;
        var operatingExpenses = Of(StatementSection.OperatingExpenses);
        var operatingProfit = grossProfit - operatingExpenses;
        var otherIncome = Of(StatementSection.OtherIncome);
        var otherExpenses = Of(StatementSection.OtherExpenses);
        var profitBeforeTax = operatingProfit + otherIncome - otherExpenses;
        var incomeTax = Of(StatementSection.IncomeTax);
        return new IncomeStatementTotalsResponse(revenue, costOfSales, grossProfit, operatingExpenses, operatingProfit, otherIncome,
            otherExpenses, profitBeforeTax, incomeTax, profitBeforeTax - incomeTax);
    }
}
