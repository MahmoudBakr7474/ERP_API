using WarehouseERP.Accounting.Application.Features.SourcePostings;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.Inventory.Contracts;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Purchasing.Contracts;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Domain;
using LinePrice = WarehouseERP.Sales.Domain.LinePrice;
using UnbilledDeliveries = WarehouseERP.Sales.Domain.UnbilledDeliveries;
using UninvoicedReceipts = WarehouseERP.Purchasing.Domain.UninvoicedReceipts;

namespace WarehouseERP.UnitTests.Accounting;

/// <summary>
/// The posting rules of operational documents (Accounting Milestone C) without a database: each document's journal
/// balances by its own lines on the right roles, stock values are rounded once per document, clearing values move per order
/// line so they return exactly to zero, failures are retried later, automatic journals are never reversed by hand, and the
/// activation happens once.
/// </summary>
public sealed class SourcePostingRulesTests
{
    private static readonly Guid Branch = Guid.NewGuid();
    private static readonly Guid Supplier = Guid.NewGuid();
    private static readonly Guid Customer = Guid.NewGuid();
    private static readonly DateTime Now = new(2027, 2, 1, 10, 0, 0, DateTimeKind.Utc);

    private static List<(PostingRole? Role, decimal Amount)> Lines(SourceEntry entry)
    {
        Assert.Equal(0m, entry.Lines.Sum(l => l.Amount));
        return entry.Lines.Where(l => l.Amount != 0m).Select(l => (l.Role, l.Amount)).ToList();
    }

    private static InventoryDocumentValue Stock(
        StockDocumentType type, decimal valueIn, decimal valueOut, AdjustmentReason? reason = null, StockDocumentType? reversed = null) =>
        new(Guid.NewGuid(), type, "ADJ-1", Branch, Now, reason, reversed, reversed is null ? null : "ISS-9", valueIn, valueOut);

    [Fact]
    public void Inventory_documents_post_stock_against_the_account_of_their_kind_rounded_once()
    {
        var found = SourceEntries.InventoryDocument(Stock(StockDocumentType.Adjustment, 10.005m, 0m, AdjustmentReason.Found), 2).Value;
        Assert.Equal(JournalType.InventoryJournal, found.JournalType);
        Assert.Equal([(PostingRole.Inventory, 10.01m), (PostingRole.InventoryGains, -10.01m)], Lines(found));
        Assert.Equal(10.005m, found.StockValue);
        Assert.Equal(10.01m, found.PostedStockValue);

        var damaged = SourceEntries.InventoryDocument(Stock(StockDocumentType.Adjustment, 0m, 4.4449m, AdjustmentReason.Damage), 2).Value;
        Assert.Equal([(PostingRole.InventoryLosses, 4.44m), (PostingRole.Inventory, -4.44m)], Lines(damaged));

        var corrected = SourceEntries.InventoryDocument(Stock(StockDocumentType.Adjustment, 3m, 2m, AdjustmentReason.Correction), 2).Value;
        Assert.Equal([(PostingRole.Inventory, 3m), (PostingRole.InventoryAdjustments, -3m), (PostingRole.InventoryAdjustments, 2m),
            (PostingRole.Inventory, -2m)], Lines(corrected));

        Assert.Equal([(PostingRole.Inventory, 7m), (PostingRole.OpeningBalanceEquity, -7m)],
            Lines(SourceEntries.InventoryDocument(Stock(StockDocumentType.OpeningBalance, 7m, 0m), 2).Value));
        Assert.Equal([(PostingRole.Inventory, 5m), (PostingRole.UnmatchedReceipts, -5m)],
            Lines(SourceEntries.InventoryDocument(Stock(StockDocumentType.Receipt, 5m, 0m), 2).Value));
        Assert.Equal([(PostingRole.StockCountDifferences, 1m), (PostingRole.Inventory, -1m)],
            Lines(SourceEntries.InventoryDocument(Stock(StockDocumentType.CountAdjustment, 0m, 1m), 2).Value));

        // A reversal of an issue brings the goods back against internal use, the other way round.
        var reversal = SourceEntries.InventoryDocument(Stock(StockDocumentType.Reversal, 6m, 0m, reversed: StockDocumentType.Issue), 2).Value;
        Assert.Equal([(PostingRole.Inventory, 6m), (PostingRole.InternalIssues, -6m)], Lines(reversal));
        Assert.Contains("ISS-9", reversal.Description, StringComparison.Ordinal);

        Assert.Equal("Accounting.Source.UnsupportedDocument",
            SourceEntries.InventoryDocument(Stock(StockDocumentType.TransferDispatch, 1m, 1m), 2).Error!.Code);
    }

    [Fact]
    public void Purchases_go_through_goods_received_not_invoiced_and_keep_price_differences_under_review()
    {
        var receipt = SourceEntries.GoodsReceipt(
            new GoodsReceiptAccounting(Guid.NewGuid(), "GRN-1", Supplier, Branch, Now, Guid.NewGuid(), 1000m), stockValue: 1000.0004m);
        Assert.Equal([(PostingRole.Inventory, 1000m), (PostingRole.GoodsReceivedNotInvoiced, -1000m)], Lines(receipt));
        Assert.Equal((1000.0004m, 1000m), (receipt.StockValue!.Value, receipt.PostedStockValue!.Value));

        var invoice = SourceEntries.SupplierInvoice(new SupplierInvoiceAccounting(
            Guid.NewGuid(), "SIN-1", "A-77", Supplier, Branch, new DateOnly(2027, 2, 1), new DateOnly(2027, 3, 3), Now, 1050m, 147m, 1197m,
            1000m, 50m));
        Assert.Equal([(PostingRole.GoodsReceivedNotInvoiced, 1000m), (PostingRole.PurchaseVarianceUnderReview, 50m), (PostingRole.InputVat, 147m),
            (PostingRole.AccountsPayable, -1197m)], Lines(invoice));
        var payable = invoice.Lines.Single(l => l.Role == PostingRole.AccountsPayable);
        Assert.Equal((PartyType.Supplier, Supplier, new DateOnly(2027, 3, 3)), (payable.PartyType!.Value, payable.PartyId!.Value, payable.DueDate!.Value));
        Assert.Equal(50m, invoice.VarianceAmount);

        // 2 units back before invoicing (received at 100), 1 after (billed 105 + 14.70 VAT); they leave stock at an average of 102.
        var supplierReturn = SourceEntries.SupplierReturn(new SupplierReturnAccounting(
            Guid.NewGuid(), "SRN-1", Supplier, Branch, Now, Guid.NewGuid(), 200m, 105m, 14.7m, 119.7m), stockValue: -306m, 2);
        Assert.Equal([(PostingRole.AccountsPayable, 119.7m), (PostingRole.GoodsReceivedNotInvoiced, 200m), (PostingRole.PurchaseVarianceUnderReview, 1m),
            (PostingRole.InputVat, -14.7m), (PostingRole.Inventory, -306m)], Lines(supplierReturn));
        Assert.Null(supplierReturn.Lines.Single(l => l.Role == PostingRole.AccountsPayable).DueDate);
        Assert.Equal(-306m, supplierReturn.PostedStockValue);
    }

    [Fact]
    public void Sales_recognize_revenue_on_delivery_and_the_invoice_only_reclassifies_it()
    {
        var delivery = SourceEntries.SalesDelivery(
            new SalesDeliveryAccounting(Guid.NewGuid(), "SDL-1", Customer, Branch, Now, Guid.NewGuid(), 900m), stockValue: -600.0049m, 2);
        Assert.Equal(JournalType.SalesJournal, delivery.JournalType);
        Assert.Equal([(PostingRole.UnbilledReceivables, 900m), (PostingRole.SalesRevenue, -900m), (PostingRole.CostOfSales, 600m),
            (PostingRole.Inventory, -600m)], Lines(delivery));

        var invoice = SourceEntries.SalesInvoice(new SalesInvoiceAccounting(
            Guid.NewGuid(), "SIV-1", Customer, Branch, new DateOnly(2027, 2, 1), new DateOnly(2027, 3, 3), Now, 900m, 126m, 1026m, 900m));
        Assert.Equal([(PostingRole.AccountsReceivable, 1026m), (PostingRole.UnbilledReceivables, -900m), (PostingRole.OutputVat, -126m)],
            Lines(invoice));

        // An invoice whose net differs from the delivered value by rounding puts only that cent on revenue.
        var rounded = SourceEntries.SalesInvoice(new SalesInvoiceAccounting(
            Guid.NewGuid(), "SIV-2", Customer, Branch, new DateOnly(2027, 2, 1), new DateOnly(2027, 3, 3), Now, 100m, 14m, 114m, 99.99m));
        Assert.Equal([(PostingRole.AccountsReceivable, 114m), (PostingRole.UnbilledReceivables, -99.99m), (PostingRole.SalesRevenue, -0.01m),
            (PostingRole.OutputVat, -14m)], Lines(rounded));

        var salesReturn = SourceEntries.SalesReturn(new SalesReturnAccounting(
            Guid.NewGuid(), "SRT-1", Customer, Branch, Now, Guid.NewGuid(), 300m, 100m, 14m, 114m), stockValue: 260m, 2);
        Assert.Equal([(PostingRole.SalesReturns, 400m), (PostingRole.OutputVat, 14m), (PostingRole.AccountsReceivable, -114m),
            (PostingRole.UnbilledReceivables, -300m), (PostingRole.Inventory, 260m), (PostingRole.CostOfSales, -260m)], Lines(salesReturn));
        Assert.Equal(PartyType.Customer, salesReturn.Lines.Single(l => l.Role == PostingRole.AccountsReceivable).PartyType);
    }

    [Fact]
    public void Pos_posts_each_transaction_and_a_closed_shift_settles_its_cash_only()
    {
        var sale = SourceEntries.PosSale(new PosSaleAccounting(Guid.NewGuid(), "PSL-1", Branch, Guid.NewGuid(), Now, Guid.NewGuid(), 100m, 14m,
            64m, 50m), stockValue: -70m, 2);
        Assert.Equal([(PostingRole.PosTillClearing, 64m), (PostingRole.CardClearing, 50m), (PostingRole.PosRevenue, -100m), (PostingRole.OutputVat, -14m),
            (PostingRole.CostOfSales, 70m), (PostingRole.Inventory, -70m)], Lines(sale));

        var refund = SourceEntries.PosReturn(new PosReturnAccounting(Guid.NewGuid(), "PSR-1", "PSL-1", Branch, Guid.NewGuid(), Now, null, 50m,
            7m, 57m, 0m), stockValue: null, 2);
        Assert.Equal([(PostingRole.PosReturns, 50m), (PostingRole.OutputVat, 7m), (PostingRole.PosTillClearing, -57m)], Lines(refund));
        Assert.Null(refund.StockValue);

        var cashbox = Guid.NewGuid();
        var shortShift = SourceEntries.PosShift(new PosShiftAccounting(Guid.NewGuid(), "SH-1", Branch, Guid.NewGuid(), Now, 500m, 595m, 600m,
            -5m, false), cashbox);
        Assert.Equal([(null, 495m), (PostingRole.PosTillClearing, -500m), (PostingRole.CashOverShort, 5m)], Lines(shortShift));
        Assert.Equal(cashbox, shortShift.Lines[0].AccountId);

        var over = SourceEntries.PosShift(new PosShiftAccounting(Guid.NewGuid(), "SH-2", Branch, Guid.NewGuid(), Now, 0m, 102m, 100m, 2m,
            true), cashbox);
        Assert.Equal([(null, 2m), (PostingRole.CashOverShort, -2m)], Lines(over));
    }

    [Fact]
    public void Clearing_values_move_per_order_line_so_any_split_returns_exactly_to_zero()
    {
        // GRNI at 0.33333333 a unit: received 3 then 2, invoiced 1 then 4.
        const decimal price = 0.33333333m;
        var received = UninvoicedReceipts.Change(0m, 3m, price, 2) + UninvoicedReceipts.Change(3m, 2m, price, 2);
        var invoiced = UninvoicedReceipts.Change(5m, -1m, price, 2) + UninvoicedReceipts.Change(4m, -4m, price, 2);
        Assert.Equal((1.67m, -1.67m), (received, invoiced));

        // Unbilled at 9.99 less 10% (an invoice line's net): delivered 1, 1, 1; invoiced 3 at once.
        var linePrice = new LinePrice(9.99m, 10m, SalesTaxTreatment.Standard, 14m);
        var delivered = UnbilledDeliveries.Change(0m, 1m, linePrice, 2) + UnbilledDeliveries.Change(1m, 1m, linePrice, 2)
                        + UnbilledDeliveries.Change(2m, 1m, linePrice, 2);
        Assert.Equal(26.97m, delivered);
        Assert.Equal(-delivered, UnbilledDeliveries.Change(3m, -3m, linePrice, 2));
        Assert.Equal(0m, UnbilledDeliveries.Value(0m, linePrice, 2));
    }

    [Fact]
    public void A_failed_document_is_retried_later_and_a_settled_one_never_changes()
    {
        var posting = SourcePosting.Discover(SourceDocumentType.SalesInvoice, Guid.NewGuid(), "SIV-1", Now, new DateOnly(2027, 2, 1), Now);
        Assert.Equal((SourcePostingStatus.Pending, Now), (posting.Status, posting.NextAttemptAtUtc!.Value));

        posting.MarkFailed("Accounting.Mapping.RoleNotMapped", "Map it.", Now);
        Assert.Equal((SourcePostingStatus.Failed, 1, Now.AddMinutes(1)), (posting.Status, posting.Attempts, posting.NextAttemptAtUtc!.Value));
        posting.MarkFailed("Accounting.Mapping.RoleNotMapped", "Map it.", Now);
        posting.MarkFailed("Accounting.Mapping.RoleNotMapped", "Map it.", Now);
        Assert.Equal(Now.AddMinutes(4), posting.NextAttemptAtUtc);
        for (var i = 0; i < 10; i++)
            posting.MarkFailed("x", new string('m', 2000), Now);
        Assert.Equal(Now + SourcePosting.MaxRetryDelay, posting.NextAttemptAtUtc);
        Assert.Equal(SourcePosting.MaxErrorMessageLength, posting.ErrorMessage!.Length);

        var journalId = Guid.NewGuid();
        posting.MarkPosted(journalId, "SL-2027-000001", null, null, null, Now);
        Assert.Equal((SourcePostingStatus.Posted, journalId, (string?)null, (DateTime?)null),
            (posting.Status, posting.JournalId!.Value, posting.ErrorCode, posting.NextAttemptAtUtc));
        Assert.Throws<InvalidOperationException>(() => posting.MarkFailed("x", "y", Now));
        Assert.Throws<InvalidOperationException>(() => posting.MarkNothingToPost(null, Now));
    }

    [Fact]
    public void Automatic_journals_are_numbered_per_module_posted_by_the_system_and_never_reversed_by_hand()
    {
        Assert.Equal(["IV", "PU", "SL", "PS"],
            new[] { JournalType.InventoryJournal, JournalType.PurchasingJournal, JournalType.SalesJournal, JournalType.PosJournal }
                .Select(JournalNumbers.Prefix));
        var lines = new List<JournalLineInput> { new(Guid.NewGuid(), Branch, 10m, 0m, null), new(Guid.NewGuid(), Branch, 0m, 10m, null) };
        var journal = JournalEntry.CreateFromSource(JournalType.SalesJournal, SourceDocumentType.SalesDelivery, Guid.NewGuid(),
            new DateOnly(2027, 2, 1), "تسليم", "SDL-1", "EGP", lines, Now).Value;
        Assert.True(journal.LinesFixed);
        journal.MarkPostedFromSource(new DocumentNumber("SL-2027-000001", Guid.NewGuid(), 1), Guid.NewGuid(), Now);
        Assert.Equal((JournalStatus.Posted, (Guid?)null), (journal.Status, journal.PostedByUserId));
        Assert.Equal(JournalEntryDomainErrors.NotReversible,
            JournalEntry.CreateReversal(journal, new DateOnly(2027, 2, 2), null, null, false, null, Now).Error);
        Assert.Throws<ArgumentOutOfRangeException>(() => JournalEntry.CreateFromSource(JournalType.Manual, SourceDocumentType.SalesDelivery,
            Guid.NewGuid(), new DateOnly(2027, 2, 1), "x", "y", "EGP", lines, Now));
    }

    [Fact]
    public void Accounting_activates_once_after_the_cutover_started_and_only_a_cashbox_receives_pos_cash()
    {
        var settings = (AccountingSettings)Activator.CreateInstance(typeof(AccountingSettings), nonPublic: true)!;
        var start = Now.AddHours(1); // the cutover has not started yet
        Assert.Equal(AccountingSettingsDomainErrors.CutoverNotStarted, settings.Activate(Guid.NewGuid(), Now, start).Error);
        Assert.False(settings.IsActivated);
        Assert.True(settings.Activate(Guid.NewGuid(), Now, Now.AddDays(-1)).IsSuccess);
        Assert.Equal((true, Now, Now.AddDays(-1)), (settings.IsActivated, settings.ActivatedAtUtc!.Value, settings.CutoverStartUtc!.Value));
        Assert.Equal(AccountingSettingsDomainErrors.AlreadyActivated, settings.Activate(Guid.NewGuid(), Now, Now.AddDays(-1)).Error);

        var assets = Account.Create("12", "أصول", null, null, AccountType.Asset, null, false, false, null).Value;
        typeof(MasterEntity).GetProperty("Id")!.SetValue(assets, Guid.NewGuid());
        var cash = Account.Create("1204", "نقدية", null, assets, AccountType.Asset, StatementSection.CurrentAssets, true, true, null).Value;
        Assert.True(CashAccount.Create("BOX-1", "خزينة", CashAccountKind.Cashbox, Branch, cash, null, null, false, receivesPosCash: true).Value
            .ReceivesPosCash);
        Assert.Equal(CashAccountDomainErrors.PosCashNotCashbox,
            CashAccount.Create("BANK-1", "بنك", CashAccountKind.Bank, Branch, cash, "NBE", "1", false, receivesPosCash: true).Error);
    }
}
