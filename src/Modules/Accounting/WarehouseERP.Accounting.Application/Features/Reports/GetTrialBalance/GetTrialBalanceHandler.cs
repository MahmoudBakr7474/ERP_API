using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetTrialBalance;

// From posted lines only. Opening = everything before the start date (a closed year's revenue and expenses are already in
// retained earnings through its closing journal); headers sum their posting accounts; each balance is shown on its side.
public sealed class GetTrialBalanceHandler(
    IAccountReadStore accounts, ILedgerReadStore ledger, AccountingReferences references, ICurrentUser currentUser)
    : IQueryHandler<GetTrialBalanceQuery, TrialBalanceResponse>
{
    public async Task<Result<TrialBalanceResponse>> HandleAsync(GetTrialBalanceQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var chart = new ReportAccounts(await accounts.ListAllAsync(ct));
        var opening = (await ledger.TotalsAsync(null, query.FromDate.AddDays(-1), query.BranchId, excludeYearEndClosing: false, ct))
            .ToDictionary(t => t.AccountId);
        var period = (await ledger.TotalsAsync(query.FromDate, query.ToDate, query.BranchId, excludeYearEndClosing: false, ct))
            .ToDictionary(t => t.AccountId);

        var sums = new Dictionary<Guid, (decimal Opening, decimal Debit, decimal Credit)>();
        foreach (var account in chart.All.Where(a => a.IsPosting))
        {
            var open = opening.GetValueOrDefault(account.Id);
            var moved = period.GetValueOrDefault(account.Id);
            var own = (Opening: (open?.Debit ?? 0m) - (open?.Credit ?? 0m), Debit: moved?.Debit ?? 0m, Credit: moved?.Credit ?? 0m);
            foreach (var target in chart.Ancestors(account).Prepend(account))
            {
                var current = sums.GetValueOrDefault(target.Id);
                sums[target.Id] = (current.Opening + own.Opening, current.Debit + own.Debit, current.Credit + own.Credit);
            }
        }

        var rows = new List<TrialBalanceRowResponse>();
        foreach (var account in chart.All)
        {
            if (query.Level is { } level && account.Level > level)
                continue;
            var (openNet, debit, credit) = sums.GetValueOrDefault(account.Id);
            if (!query.IncludeZero && openNet == 0m && debit == 0m && credit == 0m)
                continue;
            var closeNet = openNet + debit - credit;
            rows.Add(new TrialBalanceRowResponse(account.Id, account.Code, account.NameAr, account.NameEn, account.ParentId, account.Level,
                account.Type, account.IsPosting, Math.Max(openNet, 0m), Math.Max(-openNet, 0m), debit, credit, Math.Max(closeNet, 0m),
                Math.Max(-closeNet, 0m)));
        }

        var posting = chart.All.Where(a => a.IsPosting).Select(a => sums.GetValueOrDefault(a.Id)).ToList();
        var openingDebit = posting.Sum(s => Math.Max(s.Opening, 0m));
        var openingCredit = posting.Sum(s => Math.Max(-s.Opening, 0m));
        var periodDebit = posting.Sum(s => s.Debit);
        var periodCredit = posting.Sum(s => s.Credit);
        var closingDebit = posting.Sum(s => Math.Max(s.Opening + s.Debit - s.Credit, 0m));
        var closingCredit = posting.Sum(s => Math.Max(-(s.Opening + s.Debit - s.Credit), 0m));
        return new TrialBalanceResponse(query.FromDate, query.ToDate, query.BranchId, query.Level, company.Value.BaseCurrencyCode, rows,
            openingDebit, openingCredit, periodDebit, periodCredit, closingDebit, closingCredit,
            openingDebit == openingCredit && periodDebit == periodCredit && closingDebit == closingCredit);
    }
}
