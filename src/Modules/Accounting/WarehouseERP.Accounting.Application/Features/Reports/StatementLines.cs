using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Contracts.Accounts;
using WarehouseERP.Accounting.Application.Contracts.Reports;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Reports;

// Statement lines from ledger totals: one per posting account of the wanted sections that has an amount (or a comparative
// amount), signed the section's natural way, in code order.
internal static class StatementLines
{
    /// <summary>Debit minus credit for assets and expenses, credit minus debit for liabilities, equity and income.</summary>
    public static decimal Signed(AccountSummaryResponse account, AccountTotals? totals)
    {
        if (totals is null)
            return 0m;
        return account.Type is AccountType.Asset or AccountType.Expense ? totals.Debit - totals.Credit : totals.Credit - totals.Debit;
    }

    public static IReadOnlyList<StatementLineResponse> Build(
        ReportAccounts chart, IReadOnlySet<StatementSection> sections, IReadOnlyDictionary<Guid, AccountTotals> totals,
        IReadOnlyDictionary<Guid, AccountTotals>? comparative)
    {
        var lines = new List<StatementLineResponse>();
        foreach (var account in chart.All.Where(a => a.IsPosting && a.Section is { } s && sections.Contains(s)))
        {
            var amount = Signed(account, totals.GetValueOrDefault(account.Id));
            decimal? compared = comparative is null ? null : Signed(account, comparative.GetValueOrDefault(account.Id));
            if (amount == 0m && (compared ?? 0m) == 0m)
                continue;
            var parent = account.ParentId is { } parentId ? chart.Find(parentId) : null;
            lines.Add(new StatementLineResponse(account.Id, account.Code, account.NameAr, account.NameEn, account.Section!.Value,
                account.ParentId, parent?.Code, parent?.NameAr, amount, compared));
        }
        return lines;
    }
}
