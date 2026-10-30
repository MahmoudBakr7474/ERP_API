using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Reports.GetSubledgerReconciliation;

// Every control account (party type set) whether or not it has postings: its ledger balance must equal its open items'
// amounts and what is still open of them, with no line lacking a party. It reports differences; it never corrects anything.
public sealed class GetSubledgerReconciliationHandler(ISubledgerReadStore subledger, IAccountReadStore accounts, ICurrentUser currentUser)
    : IQueryHandler<GetSubledgerReconciliationQuery, SubledgerReconciliationResponse>
{
    public async Task<Result<SubledgerReconciliationResponse>> HandleAsync(GetSubledgerReconciliationQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var totals = (await subledger.ControlTotalsAsync(query.AsOfDate, ct)).ToDictionary(t => t.AccountId);
        var rows = (await accounts.ListAllAsync(ct)).Where(a => a.PartyType is not null).Select(a =>
        {
            var t = totals.GetValueOrDefault(a.Id) ?? new ControlAccountTotals(a.Id, 0m, 0m, 0m, 0m);
            var difference = t.LedgerBalance - t.ItemsAmount;
            return new SubledgerAccountResponse(a.Id, a.Code, a.NameAr, a.PartyType!.Value, t.LedgerBalance, t.ItemsAmount, t.ItemsOpenAmount,
                t.LinesWithoutParty, difference, difference == 0m && t.ItemsAmount == t.ItemsOpenAmount && t.LinesWithoutParty == 0m);
        }).ToList();
        return new SubledgerReconciliationResponse(query.AsOfDate, rows, rows.All(r => r.IsReconciled));
    }
}
