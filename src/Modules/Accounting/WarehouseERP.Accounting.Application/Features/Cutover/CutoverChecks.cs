using Microsoft.Extensions.Options;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.FiscalYears;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Contracts.Cutover;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Cutover;

// What must hold before automatic accounting starts (V1 decision 6): the cutover's start has passed (by the scan overlap, so
// documents stamped just before it have committed); the cutover's period exists and is open; every role
// the posting rules use is mapped; no opening-balance journal is left unposted; customers' and suppliers' opening items
// reconcile exactly with their control accounts; and the ledger's opening balances of the system-fed accounts equal what
// the modules hold at the cutover's start: Inventory's valuation (rounded to the minor unit, stock counted on its document's
// side of the start: CutoverBoundary), goods received not invoiced, goods delivered not invoiced, and the cash the tills had
// taken and not handed in. Documents posted from the cutover's start are journaled automatically, everything before is in
// these openings, so nothing counts twice. Once activated, the start it was checked against is the one kept in the settings.
public sealed class CutoverChecks(
    IAccountingSettingsRepository settings, AccountingReferences references, IFiscalYearRepository years, IAccountMappingRepository mappings,
    IJournalRepository journals, ILedgerReadStore ledger, ISubledgerReadStore subledgers, IInventoryAccountingSource inventory,
    IPurchasingAccountingSource purchasing, ISalesAccountingSource sales, IPosAccountingSource pos, CutoverBoundary boundary,
    IOptions<AccountingIntegrationOptions> options, TimeProvider clock)
{
    /// <summary>The roles the posting rules of operational documents use.</summary>
    public static IReadOnlyList<PostingRole> RequiredRoles { get; } =
    [
        PostingRole.OpeningBalanceEquity, PostingRole.Inventory, PostingRole.GoodsReceivedNotInvoiced, PostingRole.PurchaseVarianceUnderReview,
        PostingRole.AccountsReceivable, PostingRole.UnbilledReceivables, PostingRole.AccountsPayable, PostingRole.InputVat,
        PostingRole.OutputVat, PostingRole.SalesRevenue, PostingRole.SalesReturns, PostingRole.PosRevenue, PostingRole.PosReturns,
        PostingRole.CostOfSales, PostingRole.InventoryLosses, PostingRole.InventoryGains, PostingRole.InventoryAdjustments,
        PostingRole.StockCountDifferences, PostingRole.UnmatchedReceipts, PostingRole.InternalIssues, PostingRole.CashOverShort,
        PostingRole.CardClearing, PostingRole.PosTillClearing,
    ];

    public async Task<Result<CutoverReadinessResponse>> CheckAsync(CancellationToken ct)
    {
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var policies = await settings.GetAsync(ct);
        var cutover = policies.CutoverDate;
        var cutoverStart = policies.CutoverStartUtc ?? BusinessDates.StartUtc(cutover, BusinessDates.ZoneOf(company.Value));
        var decimals = Money.CurrencyDecimals(company.Value.BaseCurrencyCode);
        var checks = new List<CutoverCheckResponse>();

        var settledFrom = cutoverStart + options.Value.ScanOverlap;
        var started = clock.GetUtcNow().UtcDateTime >= settledFrom;
        checks.Add(new CutoverCheckResponse("CutoverStarted", started, null, null,
            started ? null : $"Activation opens once the accounting start date has begun and its first documents have committed, at {settledFrom:u}."));

        var period = await years.FindPeriodAsync(cutover, ct);
        var periodOpen = period is not null && await years.PeriodStatusAsync(period.PeriodId, ct) == FiscalPeriodStatus.Open;
        checks.Add(new CutoverCheckResponse("FiscalPeriodOpen", periodOpen, null, null,
            periodOpen ? null : "Create the fiscal year of the accounting start date and keep its first period open."));

        var mapped = (await mappings.ListAsync(ct)).ToDictionary(m => m.Role, m => m.AccountId);
        var unmapped = RequiredRoles.Where(r => !mapped.ContainsKey(r)).ToList();
        checks.Add(new CutoverCheckResponse("PostingRolesMapped", unmapped.Count == 0, null, null,
            unmapped.Count == 0 ? null : "Map these posting roles: " + string.Join(", ", unmapped) + "."));

        var openDrafts = await journals.CountOpenOfTypeAsync(JournalType.OpeningBalance, ct);
        checks.Add(new CutoverCheckResponse("OpeningBalancesPosted", openDrafts == 0, null, null,
            openDrafts == 0 ? null : $"{openDrafts} opening-balance journal(s) are drafts or waiting for approval: post or cancel them."));

        var controls = await subledgers.ControlTotalsAsync(cutover, ct);
        var unreconciled = controls.Count(c => c.LedgerBalance != c.ItemsAmount || c.ItemsAmount != c.ItemsOpenAmount || c.LinesWithoutParty != 0m);
        checks.Add(new CutoverCheckResponse("SubledgersReconciled", unreconciled == 0, null, null,
            unreconciled == 0 ? null : $"{unreconciled} customer or supplier control account(s) don't agree with their open items at the cutover."));

        var opening = (await ledger.OpeningTotalsAsync(ct)).ToDictionary(t => t.AccountId, t => t.Debit - t.Credit);
        decimal Opening(PostingRole role) => mapped.TryGetValue(role, out var id) ? opening.GetValueOrDefault(id) : 0m;
        var valuation = decimal.Round(
            await inventory.ValuationBeforeAsync(cutoverStart, ct) + await boundary.StockCorrectionAsync(cutoverStart, ct), decimals,
            MidpointRounding.AwayFromZero);
        checks.Add(Compare("InventoryValuation", valuation, Opening(PostingRole.Inventory),
            "The inventory account's opening balance must equal Inventory's valuation at the cutover."));
        checks.Add(Compare("GoodsReceivedNotInvoiced", await purchasing.ReceivedNotInvoicedValueBeforeAsync(cutoverStart, ct),
            -Opening(PostingRole.GoodsReceivedNotInvoiced),
            "The opening credit of goods received not invoiced must equal the goods received and not invoiced at the cutover."));
        checks.Add(Compare("UnbilledReceivables", await sales.UnbilledValueBeforeAsync(cutoverStart, ct), Opening(PostingRole.UnbilledReceivables),
            "The opening balance of unbilled receivables must equal the goods delivered and not invoiced at the cutover."));
        checks.Add(Compare("PosTillCash", await pos.TillCashBeforeAsync(cutoverStart, ct), Opening(PostingRole.PosTillClearing),
            "The opening balance of the tills' cash clearing must equal the cash of shifts still open at the cutover."));

        var ready = !policies.IsActivated && checks.All(c => c.Passed);
        return new CutoverReadinessResponse(cutover, cutoverStart, policies.IsActivated, policies.ActivatedAtUtc, policies.ActivatedByUserId,
            ready, checks, Convert.ToBase64String(policies.RowVersion));
    }

    private static CutoverCheckResponse Compare(string code, decimal expected, decimal actual, string message) =>
        new(code, expected == actual, expected, actual, expected == actual ? null : message);
}
