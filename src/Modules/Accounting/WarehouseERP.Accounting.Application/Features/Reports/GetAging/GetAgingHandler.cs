using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetAging;

// From open items as of the date (allocations dated later are not yet applied): what each party owes or is owed, aged by
// days past due. Customers' balances read debit-positive, suppliers' credit-positive.
public sealed class GetAgingHandler(
    ISubledgerReadStore subledger, PartyDirectory parties, AccountingReferences references, ICurrentUser currentUser)
    : IQueryHandler<GetAgingQuery, AgingResponse>
{
    public async Task<Result<AgingResponse>> HandleAsync(GetAgingQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var sign = query.PartyType == PartyType.Customer ? 1m : -1m;
        var items = await subledger.OpenItemsAsOfAsync(query.PartyType, query.AsOfDate, query.BranchId, ct);
        var named = await parties.FindAsync(items.Select(i => (i.PartyType, i.PartyId)), ct);
        var rows = items.GroupBy(i => i.PartyId).Select(g =>
        {
            decimal notDue = 0, d30 = 0, d60 = 0, d90 = 0, over = 0, unallocated = 0;
            foreach (var item in g)
            {
                var amount = item.OpenAmount * sign;
                if (amount < 0m)
                {
                    unallocated += amount;
                    continue;
                }
                var days = query.AsOfDate.DayNumber - item.DueDate.DayNumber;
                if (days <= 0) notDue += amount;
                else if (days <= 30) d30 += amount;
                else if (days <= 60) d60 += amount;
                else if (days <= 90) d90 += amount;
                else over += amount;
            }
            var party = named.GetValueOrDefault((query.PartyType, g.Key));
            return new AgingRowResponse(query.PartyType, g.Key, party?.Code, party?.NameAr, notDue, d30, d60, d90, over, unallocated,
                notDue + d30 + d60 + d90 + over + unallocated);
        }).OrderBy(r => r.PartyCode).ToList();
        return new AgingResponse(query.PartyType, query.AsOfDate, query.BranchId, company.Value.BaseCurrencyCode, rows, rows.Sum(r => r.NotDue),
            rows.Sum(r => r.Days1To30), rows.Sum(r => r.Days31To60), rows.Sum(r => r.Days61To90), rows.Sum(r => r.Over90Days),
            rows.Sum(r => r.Unallocated), rows.Sum(r => r.Total));
    }
}
