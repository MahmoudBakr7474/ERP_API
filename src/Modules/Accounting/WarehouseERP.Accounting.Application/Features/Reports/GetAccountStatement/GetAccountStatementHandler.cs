using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetAccountStatement;

// Opening balance before the range, then every posted line in date order with its running balance. A statement longer
// than the limit is refused with a hint to narrow it rather than silently cut.
public sealed class GetAccountStatementHandler(
    IAccountReadStore accounts, ILedgerReadStore ledger, AccountingReferences references, ICurrentUser currentUser)
    : IQueryHandler<GetAccountStatementQuery, AccountStatementResponse>
{
    public const int MaxLines = 5000;

    public async Task<Result<AccountStatementResponse>> HandleAsync(GetAccountStatementQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var chart = new ReportAccounts(await accounts.ListAllAsync(ct));
        var account = chart.Find(query.AccountId);
        if (account is null)
            return ReportErrors.AccountNotFound;
        var ids = chart.PostingAccountsUnder(account);
        var idSet = ids.ToHashSet();
        var filter = new LedgerLineFilter(query.FromDate, query.ToDate, ids, query.BranchId, null);
        if (await ledger.CountLinesAsync(filter, ct) > MaxLines)
            return ReportErrors.TooManyLines(MaxLines);

        var opening = (await ledger.TotalsAsync(null, query.FromDate.AddDays(-1), query.BranchId, excludeYearEndClosing: false, ct))
            .Where(t => idSet.Contains(t.AccountId))
            .Sum(t => t.Debit - t.Credit);
        var lines = await ledger.LinesAsync(filter, MaxLines, ct);
        var branches = await references.BranchesAsync(lines.Select(l => l.BranchId).Distinct().ToList(), ct);
        var balance = opening;
        var rows = new List<AccountStatementLineResponse>(lines.Count);
        foreach (var line in lines)
        {
            balance += line.Debit - line.Credit;
            rows.Add(new AccountStatementLineResponse(line.PostingDate, line.JournalId, line.JournalNumber, line.JournalType,
                line.Description ?? line.JournalDescription, line.Reference, line.AccountId, chart.Find(line.AccountId)?.Code ?? "",
                line.BranchId, branches.GetValueOrDefault(line.BranchId)?.Code, line.Debit, line.Credit, balance));
        }
        return new AccountStatementResponse(account.Id, account.Code, account.NameAr, account.NameEn, !account.IsPosting, query.FromDate,
            query.ToDate, query.BranchId, company.Value.BaseCurrencyCode, opening, lines.Sum(l => l.Debit), lines.Sum(l => l.Credit),
            balance, rows);
    }
}
