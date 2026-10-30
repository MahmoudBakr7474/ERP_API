using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Infrastructure;
using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.IntegrationTests.Http;

// Accounting Milestone C against SQL Server, on a database of its own (the cutover activates accounting for the whole
// company, and the checks compare it with every document the modules hold): documents posted before the cutover belong to
// the opening balances, which must agree with the modules before activation; documents posted after it are journaled once
// each by the run (on demand here, the worker in production), a failure waits in the exception queue until it is fixed, and
// the ledger reconciles with Inventory, Purchasing, Sales and POS.
public sealed class AccountingIntegrationApiTests : IAsyncLifetime
{
    private const string SourcePostings = "/api/accounting/source-postings";
    private const string Cutover = "/api/accounting/cutover";

    private static readonly string Everything = string.Join(',',
        new[] { AccountingTesting.AllPermissions, PurchasingTesting.AllPermissions, SalesTesting.AllPermissions, PosTesting.AllPermissions }
            .SelectMany(p => p.Split(',')).Distinct());

    private readonly DatabaseFixture _db = new();
    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(2025, 12, 20, 8, 0, 0, TimeSpan.Zero));
    private ApiFactory _api = null!;

    public async Task InitializeAsync()
    {
        await _db.InitializeAsync();
        _api = new ApiFactory(_db.ConnectionString, clock: _clock,
            settings: new Dictionary<string, string?> { ["Accounting:Integration:WorkerEnabled"] = "false" });
    }

    public async Task DisposeAsync()
    {
        await _api.DisposeAsync();
        await _db.DisposeAsync();
    }

    private static Task<JsonElement> Posted(Task<HttpResponseMessage> pending) => InventoryTesting.PostedAsync(pending);

    private static async Task<JsonElement> PageAsync(HttpClient client, string query) =>
        await client.GetJsonAsync($"{SourcePostings}?pageSize=100&{query}");

    private static JsonElement Single(JsonElement page, SourceDocumentType type) =>
        page.GetProperty("items").EnumerateArray().Single(i => i.Text("sourceType") == type.ToString());

    [Fact]
    public async Task Documents_after_an_activated_cutover_are_journaled_once_each_failures_wait_in_the_queue_and_the_ledger_reconciles_with_the_modules()
    {
        var admin = _api.CreateClient().As(Everything, userId: Guid.NewGuid());
        await _db.EnsureCompanyAsync();
        var room = await InventoryTesting.CreateStockroomAsync(_db, admin);
        var supplier = await PurchasingTesting.CreateSupplierAsync(admin);
        var customer = await SalesTesting.CreateCustomerAsync(admin);

        // Before the cutover (2026-01-01): 10 received at 100 and not invoiced, 4 of 6 ordered at 150 delivered and not
        // invoiced, and a till that sold one item for cash in a shift still open at the cutover.
        var (orderId, orderLines) = await PurchasingTesting.ApprovedOrderAsync(admin, supplier, room.WarehouseId,
            PurchasingTesting.OrderLine(room.ProductId, room.Piece, 10m, 100m, taxRate: 14m));
        var receiptId = (await Posted(PurchasingTesting.ReceiveAsync(admin, orderId, Guid.NewGuid(), (orderLines[0], 10m)))).GetProperty("id").GetGuid();
        var (salesOrderId, salesLines) = await SalesTesting.ConfirmedOrderAsync(admin, customer, room.WarehouseId,
            SalesTesting.Line(room.ProductId, room.Piece, 6m, 150m, taxTreatment: SalesTaxTreatment.Standard, taxRate: 14m));
        var firstDelivery = await SalesTesting.DeliverAsync(admin, salesOrderId, (salesLines[0], 4m, null));
        var till = await PosTesting.CreateTillAsync(_db, admin); // 10 pieces received at 6
        var shiftId = await PosTesting.OpenShiftAsync(admin, till.TerminalId).CreatedIdAsync();
        var item = PosTesting.Lines(PosTesting.CartLine(till.Room.ProductId, till.Room.Piece, 1m));
        var unit = (await PosTesting.CheckoutAsync(admin, till.TerminalId, item, PosTesting.Pay(PosTesting.Cash(20m))).SoldAsync()).Decimal("totalAmount");

        _clock.SetUtcNow(new DateTimeOffset(2026, 1, 15, 8, 30, 0, TimeSpan.Zero));
        (await admin.PostAsync($"{AccountingTesting.Accounts}/default-chart", null)).EnsureSuccessStatusCode();
        var chart = await AccountingTesting.ChartAsync(admin);
        await admin.PostJsonAsync(AccountingTesting.Years, new { startYear = 2026 }).CreatedIdAsync();
        async Task SetCutoverAsync(string date)
        {
            var current = await admin.GetJsonAsync(AccountingTesting.Settings);
            await admin.PutJsonAsync(AccountingTesting.Settings, new
            {
                fiscalYearStartMonth = 1, cutoverDate = date, manualJournalsRequireApproval = true, vouchersRequireApproval = false,
                rowVersion = current.Text("rowVersion"),
            }).EnsureOkAsync();
        }

        // A cutover that hasn't started can't be activated: the openings are checked against what the modules hold at its start.
        await SetCutoverAsync("2026-02-01");
        var future = (await admin.GetJsonAsync(Cutover)).GetProperty("checks").EnumerateArray().Single(c => c.Text("code") == "CutoverStarted");
        Assert.False(future.GetProperty("passed").GetBoolean());
        await SetCutoverAsync("2026-01-01");

        // Nothing runs before activation, and activation waits until the opening balances agree with the modules.
        await (await admin.PostJsonAsync($"{SourcePostings}/run", new { fullScan = false }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Source.NotActive");
        var readiness = await admin.GetJsonAsync(Cutover);
        var checks = readiness.GetProperty("checks").EnumerateArray().ToDictionary(c => c.Text("code")!);
        Assert.False(readiness.GetProperty("ready").GetBoolean());
        Assert.Equal("2025-12-31T22:00:00Z", readiness.Text("cutoverStartUtc")); // local midnight in Cairo
        Assert.True(checks["CutoverStarted"].GetProperty("passed").GetBoolean());
        Assert.True(checks["FiscalPeriodOpen"].GetProperty("passed").GetBoolean());
        Assert.True(checks["PostingRolesMapped"].GetProperty("passed").GetBoolean());
        Assert.True(checks["SubledgersReconciled"].GetProperty("passed").GetBoolean());
        Assert.Equal((654m, 0m), (checks["InventoryValuation"].Decimal("expected"), checks["InventoryValuation"].Decimal("actual")));
        Assert.Equal(1000m, checks["GoodsReceivedNotInvoiced"].Decimal("expected"));
        Assert.Equal(600m, checks["UnbilledReceivables"].Decimal("expected"));
        Assert.Equal(unit, checks["PosTillCash"].Decimal("expected"));
        await (await admin.PostJsonAsync($"{Cutover}/activate", new { rowVersion = readiness.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Cutover.NotReady");

        var branch = room.BranchId;
        var openingId = await admin.PostJsonAsync($"{AccountingTesting.Journals}/opening", new
        {
            description = "أرصدة الافتتاح", reference = "OB-1",
            lines = new[]
            {
                AccountingTesting.Line(chart["120101"], branch, debit: 654m), AccountingTesting.Line(chart["120202"], branch, debit: 600m),
                AccountingTesting.Line(chart["120403"], branch, debit: unit), AccountingTesting.Line(chart["220102"], branch, credit: 1000m),
                AccountingTesting.Line(chart["35"], branch, credit: 254m + unit),
            },
        }).CreatedIdAsync();
        await AccountingTesting.PostNowAsync(admin, openingId);
        readiness = await admin.GetJsonAsync(Cutover);
        Assert.True(readiness.GetProperty("ready").GetBoolean(), readiness.ToString());
        await admin.PostJsonAsync($"{Cutover}/activate", new { rowVersion = readiness.Text("rowVersion") }).EnsureOkAsync();
        await (await admin.PostJsonAsync($"{Cutover}/activate", new { rowVersion = (await admin.GetJsonAsync(Cutover)).Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.Settings.AlreadyActivated");
        var settings = await admin.GetJsonAsync(AccountingTesting.Settings);
        Assert.True(settings.GetProperty("cutoverLocked").GetBoolean());
        await (await admin.PutJsonAsync(AccountingTesting.Settings, new
            {
                fiscalYearStartMonth = 1, cutoverDate = "2026-02-01", manualJournalsRequireApproval = true, vouchersRequireApproval = false,
                rowVersion = settings.Text("rowVersion"),
            }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Settings.CutoverLocked");

        // The openings activation was checked against no longer change: no new opening journal, no reversal of one.
        await (await admin.PostJsonAsync($"{AccountingTesting.Journals}/opening", new
            {
                description = "متأخر", reference = "OB-2",
                lines = new[] { AccountingTesting.Line(chart["120101"], branch, debit: 1m), AccountingTesting.Line(chart["35"], branch, credit: 1m) },
            }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.OpeningAfterActivation");
        await (await admin.PostJsonAsync($"{AccountingTesting.Journals}/{openingId}/reverse",
                new { postingDate = "2026-01-15", description = (string?)null, reference = (string?)null }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.OpeningAfterActivation");

        // After the cutover: the supplier bills 105 (a 5 a unit difference), the customer is invoiced for the 4 delivered, 2 more
        // are delivered, 3 come back (2 not invoiced, 1 invoiced), 1 goes back to the supplier, 2 are received without one, and
        // the till sells for cash and card, refunds a card sale and closes 2.80 short.
        void Tick() => _clock.Advance(TimeSpan.FromSeconds(1)); // documents post in order, as they would by real users
        var supplierInvoice = await PurchasingTesting.DraftInvoiceAsync(admin, orderId, "INV-77", PurchasingTesting.InvoiceLine(orderLines[0], 10m, 105m, 14m))
            .CreatedIdAsync();
        await PurchasingTesting.ActAsync(admin, PurchasingTesting.Invoices, supplierInvoice, "submit").EnsureOkAsync();
        await Posted(PurchasingTesting.ActAsync(admin, PurchasingTesting.Invoices, supplierInvoice, "approve", idempotencyKey: Guid.NewGuid()));
        Tick();
        var salesInvoice = await SalesTesting.DraftInvoiceAsync(admin, salesOrderId, (salesLines[0], 4m)).CreatedIdAsync();
        await Posted(SalesTesting.ActAsync(admin, SalesTesting.Invoices, salesInvoice, "post", idempotencyKey: Guid.NewGuid()));
        Tick();
        await SalesTesting.DeliverAsync(admin, salesOrderId, (salesLines[0], 2m, null));
        Tick();
        var deliveryLine = (await SalesTesting.GetAsync(admin, SalesTesting.Deliveries, firstDelivery)).GetProperty("lines")[0].GetProperty("id").GetGuid();
        var salesReturn = await SalesTesting.DraftReturnAsync(admin, firstDelivery, (deliveryLine, 3m, ReturnCondition.FitForResale)).CreatedIdAsync();
        await SalesTesting.ActAsync(admin, SalesTesting.Returns, salesReturn, "submit").EnsureOkAsync();
        await Posted(SalesTesting.ActAsync(admin, SalesTesting.Returns, salesReturn, "approve", idempotencyKey: Guid.NewGuid()));
        Tick();
        var receiptLine = (await PurchasingTesting.GetAsync(admin, PurchasingTesting.Receipts, receiptId)).GetProperty("lines")[0].GetProperty("id").GetGuid();
        var supplierReturn = await PurchasingTesting.DraftReturnAsync(admin, receiptId, (receiptLine, 1m)).CreatedIdAsync();
        await PurchasingTesting.ActAsync(admin, PurchasingTesting.Returns, supplierReturn, "submit").EnsureOkAsync();
        await Posted(PurchasingTesting.ActAsync(admin, PurchasingTesting.Returns, supplierReturn, "approve", idempotencyKey: Guid.NewGuid()));
        Tick();
        await InventoryTesting.ReceiveAsync(admin, room, 2m, 7m);
        Tick();
        await PosTesting.CheckoutAsync(admin, till.TerminalId, item, PosTesting.Pay(PosTesting.Cash(unit))).SoldAsync();
        Tick();
        var cardSale = await PosTesting.CheckoutAsync(admin, till.TerminalId,
            PosTesting.Lines(PosTesting.CartLine(till.Room.ProductId, till.Room.Piece, 2m)), PosTesting.Pay(PosTesting.Card(2 * unit))).SoldAsync();
        Tick();
        await PosTesting.ReturnAsync(admin, till.TerminalId, cardSale.GetProperty("id").GetGuid(),
            PosTesting.Lines(PosTesting.ReturnLine(cardSale.GetProperty("lines")[0].GetProperty("id").GetGuid(), 1m)),
            PosTesting.Pay(PosTesting.Card(unit))).SoldAsync();
        Tick();
        await PosTesting.CloseShiftAsync(admin, shiftId, 100m + 2 * unit - 2.80m, "عجز 2.80").SoldAsync();

        // One run finds the ten documents posted since the cutover (none from before it) and journals all but the shift: its
        // branch has no cashbox receiving POS cash yet.
        Tick();

        // Before anything is journaled, Sales' exposure already counts the invoice and the credit, from its own documents.
        var waiting = await admin.GetJsonAsync($"{SalesTesting.Customers}/{customer}/exposure");
        Assert.Equal((false, 0m, 513m), (waiting.GetProperty("isPartial").GetBoolean(), waiting.Decimal("receivableBalance"),
            waiting.Decimal("unjournaledAmount")));
        var exposureBefore = waiting.Decimal("totalExposure");

        var runAt = _clock.GetUtcNow().UtcDateTime;
        var run = await Posted(admin.PostJsonAsync($"{SourcePostings}/run", new { fullScan = false }));
        Assert.Equal((10, 9, 0, 1), (run.GetProperty("discovered").GetInt32(), run.GetProperty("posted").GetInt32(),
            run.GetProperty("nothingToPost").GetInt32(), run.GetProperty("failed").GetInt32()));
        var failed = Single(await PageAsync(admin, "status=Failed"), SourceDocumentType.PosShift);
        Assert.Equal(("Accounting.Source.PosCashboxMissing", 1, "2026-01-15"), (failed.Text("errorCode"), failed.GetProperty("attempts").GetInt32(),
            failed.Text("postingDate")));
        Assert.Equal(runAt.AddMinutes(1), failed.GetProperty("nextAttemptAtUtc").GetDateTime().ToUniversalTime());

        var cashboxAccount = await AccountingTesting.CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);
        await (await admin.PostJsonAsync(AccountingTesting.CashAccounts, new
            {
                code = DatabaseFixture.Code("BK"), name = "بنك", kind = CashAccountKind.Bank, branchId = till.Room.BranchId, accountId = cashboxAccount,
                bankName = "البنك", bankAccountNumber = "1", allowsOverdraft = false, receivesPosCash = true,
            }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.CashAccount.PosCashNotCashbox");
        await admin.PostJsonAsync(AccountingTesting.CashAccounts, new
        {
            code = DatabaseFixture.Code("CB"), name = "خزينة نقاط البيع", kind = CashAccountKind.Cashbox, branchId = till.Room.BranchId,
            accountId = cashboxAccount, bankName = (string?)null, bankAccountNumber = (string?)null, allowsOverdraft = false, receivesPosCash = true,
        }).CreatedIdAsync();
        await (await admin.PostJsonAsync(AccountingTesting.CashAccounts, new
            {
                code = DatabaseFixture.Code("CB"), name = "خزينة ثانية", kind = CashAccountKind.Cashbox, branchId = till.Room.BranchId,
                accountId = await AccountingTesting.CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets),
                bankName = (string?)null, bankAccountNumber = (string?)null, allowsOverdraft = false, receivesPosCash = true,
            }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.CashAccount.PosCashboxTaken");

        var retried = await Posted(admin.PostAsync($"{SourcePostings}/{failed.GetProperty("id").GetGuid()}/retry", null));
        Assert.Equal(("Posted", (string?)null), (retried.Text("status"), retried.Text("errorCode")));
        Assert.StartsWith("PS-2026-", retried.Text("journalNumber"), StringComparison.Ordinal);
        var shiftJournal = await AccountingTesting.JournalAsync(admin, retried.GetProperty("journalId").GetGuid());
        Assert.Equal([(cashboxAccount, 2 * unit - 2.80m, 0m, (Guid?)null), (chart["120403"], 0m, 2 * unit, null), (chart["5402"], 2.80m, 0m, null)],
            AccountingTesting.LinesOf(shiftJournal));

        // Each journal follows its rule: the invoice reclassifies the delivered revenue without recognizing it again, the supplier's
        // bill keeps its price difference under review, a return credits the customer and claims from the supplier.
        var postings = await PageAsync(admin, "status=Posted");
        var invoiceJournal = await AccountingTesting.JournalAsync(admin, Single(postings, SourceDocumentType.SalesInvoice).GetProperty("journalId").GetGuid());
        Assert.Equal(("SalesJournal", "Posted", "SalesInvoice", salesInvoice), (invoiceJournal.Text("journalType"), invoiceJournal.Text("status"),
            invoiceJournal.Text("sourceType"), invoiceJournal.GetProperty("sourceId").GetGuid()));
        Assert.Equal([(chart["120201"], 684m, 0m, customer), (chart["120202"], 0m, 600m, null), (chart["220201"], 0m, 84m, null)],
            AccountingTesting.LinesOf(invoiceJournal));
        await (await admin.PostJsonAsync($"{AccountingTesting.Journals}/{invoiceJournal.GetProperty("id").GetGuid()}/reverse",
                new { postingDate = "2026-01-15", description = (string?)null, reference = (string?)null }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.NotReversible");
        var billJournal = await AccountingTesting.JournalAsync(admin, Single(postings, SourceDocumentType.SupplierInvoice).GetProperty("journalId").GetGuid());
        Assert.Equal([(chart["220102"], 1000m, 0m, null), (chart["120503"], 50m, 0m, null), (chart["120301"], 147m, 0m, null),
            (chart["220101"], 0m, 1197m, supplier)], AccountingTesting.LinesOf(billJournal));
        var deliveryJournal = await AccountingTesting.JournalAsync(admin, Single(postings, SourceDocumentType.SalesDelivery).GetProperty("journalId").GetGuid());
        Assert.Equal([(chart["120202"], 300m, 0m, null), (chart["4101"], 0m, 300m, null), (chart["5101"], 200m, 0m, null), (chart["120101"], 0m, 200m, null)],
            AccountingTesting.LinesOf(deliveryJournal));
        var returnJournal = await AccountingTesting.JournalAsync(admin, Single(postings, SourceDocumentType.SalesReturn).GetProperty("journalId").GetGuid());
        Assert.Equal([(chart["4103"], 450m, 0m, null), (chart["220201"], 21m, 0m, null), (chart["120201"], 0m, 171m, customer), (chart["120202"], 0m, 300m, null),
            (chart["120101"], 300m, 0m, null), (chart["5101"], 0m, 300m, null)], AccountingTesting.LinesOf(returnJournal));
        var claimJournal = await AccountingTesting.JournalAsync(admin, Single(postings, SourceDocumentType.SupplierReturn).GetProperty("journalId").GetGuid());
        Assert.Equal([(chart["220101"], 119.7m, 0m, supplier), (chart["120503"], 0m, 5m, null), (chart["120301"], 0m, 14.7m, null), (chart["120101"], 0m, 100m, null)],
            AccountingTesting.LinesOf(claimJournal));
        var variances = (await PageAsync(admin, "varianceOnly=true")).GetProperty("items").EnumerateArray()
            .Select(i => i.Decimal("varianceAmount")).OrderBy(v => v).ToList();
        Assert.Equal([-5m, 50m], variances);

        // The credit and the claim are open items like any other; Sales now sees the customer's receivable.
        Assert.Equal([-171m, 684m], (await AccountingTesting.OpenItemsAsync(admin, PartyType.Customer, customer)).Select(i => i.Decimal("amount")).OrderBy(a => a));
        Assert.Equal([-1197m, 119.7m], (await AccountingTesting.OpenItemsAsync(admin, PartyType.Supplier, supplier)).Select(i => i.Decimal("amount")).OrderBy(a => a));
        var exposure = await admin.GetJsonAsync($"{SalesTesting.Customers}/{customer}/exposure");
        Assert.False(exposure.GetProperty("isPartial").GetBoolean());
        Assert.Equal((513m, 0m), (exposure.Decimal("receivableBalance"), exposure.Decimal("unjournaledAmount")));
        Assert.Equal(513m + exposure.Decimal("uninvoicedOrderAmount"), exposure.Decimal("totalExposure"));
        Assert.Equal(exposureBefore, exposure.Decimal("totalExposure")); // journaling moved the amounts, it didn't change the total

        // Running again (the worker here) finds nothing new, and a full scan from the cutover neither: each document has one journal.
        await _api.Services.GetRequiredService<SourcePostingWorker>().RunOnceAsync(default);
        var full = await Posted(admin.PostJsonAsync($"{SourcePostings}/run", new { fullScan = true }));
        Assert.Equal((0, 0), (full.GetProperty("discovered").GetInt32(), full.GetProperty("posted").GetInt32()));
        var status = await admin.GetJsonAsync($"{SourcePostings}/status");
        Assert.Equal((true, 10, 0, 0), (status.GetProperty("isActivated").GetBoolean(), status.GetProperty("posted").GetInt32(),
            status.GetProperty("pending").GetInt32(), status.GetProperty("failed").GetInt32()));
        Assert.Equal(4, status.GetProperty("cursors").GetArrayLength());

        // The ledger agrees with the modules: stock value (only rounding explained), goods received not invoiced, unbilled
        // receivables and the tills' cash; no document missing, none unmatched; the shift's shortage is listed for follow-up.
        var reconciliation = await admin.GetJsonAsync($"{SourcePostings}/reconciliation");
        Assert.True(reconciliation.GetProperty("isReconciled").GetBoolean(), reconciliation.ToString());
        var stock = reconciliation.GetProperty("inventory");
        Assert.Equal((656m, 656m, 0m, 0m), (stock.Decimal("ledgerBalance"), stock.Decimal("valuation"), stock.Decimal("unpostedValue"), stock.Decimal("unexplained")));
        Assert.All(reconciliation.GetProperty("clearingAccounts").EnumerateArray(), c => Assert.Equal(0m, c.Decimal("difference")));
        Assert.Equal((0, 0), (reconciliation.GetProperty("missingCount").GetInt32(), reconciliation.GetProperty("unmatchedJournals").GetInt32()));
        Assert.Equal(-2.80m, reconciliation.GetProperty("cashDifferences").EnumerateArray().Single().Decimal("cashDifference"));
        var subledgers = await admin.GetJsonAsync($"{AccountingTesting.Reports}/subledger-reconciliation?asOfDate=2026-01-15");
        Assert.True(subledgers.GetProperty("isReconciled").GetBoolean());

        // Reading the queue needs journals.read; running or retrying it needs accounting.integration.manage.
        var reader = _api.CreateClient().As(AccountingPermissions.JournalsRead, userId: Guid.NewGuid());
        Assert.Equal(HttpStatusCode.OK, (await reader.GetAsync($"{SourcePostings}/status")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostJsonAsync($"{SourcePostings}/run", new { fullScan = false })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await reader.PostJsonAsync($"{Cutover}/activate", new { rowVersion = "AAAAAAAAAAA=" })).StatusCode);
    }
}
