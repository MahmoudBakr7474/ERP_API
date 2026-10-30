using Microsoft.Extensions.Options;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.OpenItems.ListOpenItems;

// The subledger as a list: open items oldest due first, with their party's code and name (from Sales or Purchasing), their
// active allocations, and how overdue they are today.
public sealed class ListOpenItemsHandler(
    IOpenItemReadStore openItems, IAccountRepository accounts, PartyDirectory parties, AccountingReferences references,
    ICurrentUser currentUser, TimeProvider clock, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListOpenItemsQuery, PagedResult<OpenItemResponse>>
{
    public async Task<Result<PagedResult<OpenItemResponse>>> HandleAsync(ListOpenItemsQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime,
            TimeZoneInfo.FindSystemTimeZoneById(company.Value.TimeZoneId)));
        var page = await openItems.ListAsync(new OpenItemFilter(query.PartyType, query.PartyId, query.AccountId, query.OpenOnly),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
        var ids = page.Items.Select(i => i.Id).ToList();
        var allocations = await openItems.AllocationsOfAsync(ids, ct);
        var named = await parties.FindAsync(page.Items.Select(i => (i.PartyType, i.PartyId)), ct);
        var codes = (await accounts.FindManyAsync(page.Items.Select(i => i.AccountId).Distinct().ToList(), ct)).ToDictionary(a => a.Id, a => a.Code);
        var items = page.Items.Select(i =>
        {
            var party = named.GetValueOrDefault((i.PartyType, i.PartyId));
            var settled = allocations.Where(a => a.DebitItemId == i.Id || a.CreditItemId == i.Id)
                .Select(a => new OpenItemAllocationResponse(a.Id, a.DebitItemId == i.Id ? a.CreditItemId : a.DebitItemId, a.Amount, a.AllocatedOn,
                    a.JournalEntryId))
                .ToList();
            var overdue = i.OpenAmount == 0m ? 0 : Math.Max(0, today.DayNumber - i.DueDate.DayNumber);
            return new OpenItemResponse(i.Id, i.PartyType, i.PartyId, party?.Code, party?.NameAr, i.AccountId, codes.GetValueOrDefault(i.AccountId) ?? "",
                i.BranchId, i.JournalEntryId, i.DocumentNumber, i.JournalType, i.Reference, i.DocumentDate, i.DueDate, i.Amount, i.OpenAmount, overdue,
                settled);
        }).ToList();
        return new PagedResult<OpenItemResponse>(items, page.Page, page.PageSize, page.TotalCount);
    }
}
