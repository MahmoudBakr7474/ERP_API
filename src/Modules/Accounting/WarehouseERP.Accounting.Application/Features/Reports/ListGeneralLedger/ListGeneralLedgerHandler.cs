using Microsoft.Extensions.Options;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Reports.ListGeneralLedger;

// Posted lines page by page, in date, number and line order, filtered by account (with its posting accounts), branch and
// journal type.
public sealed class ListGeneralLedgerHandler(
    IAccountReadStore accounts, ILedgerReadStore ledger, AccountingReferences references, ICurrentUser currentUser,
    IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListGeneralLedgerQuery, PagedResult<GeneralLedgerLineResponse>>
{
    public async Task<Result<PagedResult<GeneralLedgerLineResponse>>> HandleAsync(ListGeneralLedgerQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var chart = new ReportAccounts(await accounts.ListAllAsync(ct));
        IReadOnlyList<Guid>? ids = null;
        if (query.AccountId is { } accountId)
        {
            var account = chart.Find(accountId);
            if (account is null)
                return ReportErrors.AccountNotFound;
            ids = chart.PostingAccountsUnder(account);
        }
        var page = await ledger.PageLinesAsync(new LedgerLineFilter(query.FromDate, query.ToDate, ids, query.BranchId, query.JournalType),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
        var branches = await references.BranchesAsync(page.Items.Select(l => l.BranchId).Distinct().ToList(), ct);
        var items = page.Items.Select(l =>
        {
            var account = chart.Find(l.AccountId);
            return new GeneralLedgerLineResponse(l.PostingDate, l.JournalId, l.JournalNumber, l.JournalType, l.LineNumber, l.AccountId,
                account?.Code ?? "", account?.NameAr ?? "", l.BranchId, branches.GetValueOrDefault(l.BranchId)?.Code,
                l.Description ?? l.JournalDescription, l.Reference, l.Debit, l.Credit);
        }).ToList();
        return new PagedResult<GeneralLedgerLineResponse>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
