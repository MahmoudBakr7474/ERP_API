using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;

// Everything compared now. The inventory account must equal the valuation at the cutover (rounded once) plus what journals
// posted for stock; the valuation equals the valuation at the cutover plus what every document moved: so their difference
// is the opening's rounding, the journals' rounding and what is not journaled yet, and anything else (an opening that
// differs, a journal that doesn't match its document) is unexplained. The valuation at the cutover counts stock on its
// document's side of the start (CutoverBoundary). Clearing accounts agree with their modules once every document is
// journaled. A full scan from the cutover finds documents without an accounting identity.
public sealed class GetSourceReconciliationHandler(
    IAccountingSettingsRepository settings, AccountingReferences references, IAccountMappingRepository mappings, ILedgerReadStore ledger,
    ISourcePostingReadStore readStore, SourceDocuments documents, IInventoryAccountingSource inventory,
    IPurchasingAccountingSource purchasing, ISalesAccountingSource sales, IPosAccountingSource pos, CutoverBoundary boundary,
    ICurrentUser currentUser, TimeProvider clock)
    : IQueryHandler<GetSourceReconciliationQuery, SourceReconciliationResponse>
{
    private const int MaxListed = 200;

    public async Task<Result<SourceReconciliationResponse>> HandleAsync(GetSourceReconciliationQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var policies = await settings.GetAsync(ct);
        var zone = BusinessDates.ZoneOf(company.Value);
        var cutoverStart = policies.CutoverStartUtc ?? BusinessDates.StartUtc(policies.CutoverDate, zone);
        var decimals = Money.CurrencyDecimals(company.Value.BaseCurrencyCode);
        var now = clock.GetUtcNow().UtcDateTime;

        var mapped = (await mappings.ListAsync(ct)).ToDictionary(m => m.Role, m => m.AccountId);
        var totals = (await ledger.TotalsAsync(null, DateOnly.MaxValue, null, excludeYearEndClosing: false, ct))
            .ToDictionary(t => t.AccountId, t => t.Debit - t.Credit);
        var opening = (await ledger.OpeningTotalsAsync(ct)).ToDictionary(t => t.AccountId, t => t.Debit - t.Credit);
        decimal Balance(PostingRole role) => mapped.TryGetValue(role, out var id) ? totals.GetValueOrDefault(id) : 0m;
        decimal Opening(PostingRole role) => mapped.TryGetValue(role, out var id) ? opening.GetValueOrDefault(id) : 0m;

        var valuation = await inventory.ValuationBeforeAsync(now, ct);
        var valuationAtCutover = await inventory.ValuationBeforeAsync(cutoverStart, ct) + await boundary.StockCorrectionAsync(cutoverStart, ct);
        var openingRounding = decimal.Round(valuationAtCutover, decimals, MidpointRounding.AwayFromZero) - valuationAtCutover;
        var stock = await readStore.StockTotalsAsync(ct);
        var inventoryBalance = Balance(PostingRole.Inventory);
        var difference = inventoryBalance - valuation;
        var openingDifference = Opening(PostingRole.Inventory) - valuationAtCutover;
        var rounding = stock.PostedStockValue - stock.StockValue;
        var unposted = valuation - valuationAtCutover - stock.StockValue;
        var stockReconciliation = new InventoryReconciliationResponse(inventoryBalance, valuation, difference, openingDifference, rounding,
            unposted, difference - openingRounding - rounding + unposted);

        List<ClearingAccountResponse> clearing =
        [
            Clearing(PostingRole.GoodsReceivedNotInvoiced, -Balance(PostingRole.GoodsReceivedNotInvoiced),
                await purchasing.ReceivedNotInvoicedValueBeforeAsync(now, ct)),
            Clearing(PostingRole.UnbilledReceivables, Balance(PostingRole.UnbilledReceivables), await sales.UnbilledValueBeforeAsync(now, ct)),
            Clearing(PostingRole.PosTillClearing, Balance(PostingRole.PosTillClearing), await pos.TillCashBeforeAsync(now, ct)),
        ];

        var counts = (await readStore.CountsAsync(ct)).Select(c => new SourceStatusCountResponse(c.SourceType, c.Status, c.Count)).ToList();
        var registered = await readStore.RegisteredAsync(ct);
        var missing = new List<MissingSourceResponse>();
        foreach (var module in SourceDocuments.Modules)
            missing.AddRange((await documents.ListAsync(module, cutoverStart, now, ct))
                .Where(d => !registered.Contains((d.Type, d.Id)) && d.OccurredAtUtc >= cutoverStart)
                .Select(d => new MissingSourceResponse(d.Type, d.Id, d.Number, d.OccurredAtUtc)));
        var unmatched = await readStore.UnmatchedJournalsAsync(ct);
        var incidents = (await pos.ListOpenCardIncidentsAsync(ct))
            .Select(i => new OpenCardIncidentResponse(i.Id, i.BranchId, i.TerminalId, i.ShiftId, i.CardAmount, i.References, i.OccurredAtUtc))
            .ToList();
        var differences = (await pos.ListCashDifferencesAsync(cutoverStart, ct))
            .Select(d => new CashDifferenceResponse(d.ShiftId, d.Number, d.BranchId, d.ClosedAtUtc, d.CashDifference, d.IsForceClosed))
            .ToList();

        var unsettled = counts.Where(c => c.Status is SourcePostingStatus.Pending or SourcePostingStatus.Failed).Sum(c => c.Count);
        var reconciled = policies.IsActivated && stockReconciliation.Unexplained == 0m && unposted == 0m && clearing.All(c => c.Difference == 0m)
                         && missing.Count == 0 && unsettled == 0 && unmatched == 0;
        return new SourceReconciliationResponse(
            policies.IsActivated, policies.CutoverDate, now, stockReconciliation, clearing, counts, missing.Count,
            missing.OrderBy(m => m.OccurredAtUtc).Take(MaxListed).ToList(), unmatched, incidents, differences, reconciled);
    }

    private static ClearingAccountResponse Clearing(PostingRole role, decimal ledgerBalance, decimal sourceValue) =>
        new(role, ledgerBalance, sourceValue, ledgerBalance - sourceValue);
}
