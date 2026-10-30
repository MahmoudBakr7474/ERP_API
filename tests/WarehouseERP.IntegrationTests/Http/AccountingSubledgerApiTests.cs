using System.Net;
using System.Text.Json;
using WarehouseERP.Accounting.Contracts;
using static WarehouseERP.IntegrationTests.Http.AccountingTesting;

namespace WarehouseERP.IntegrationTests.Http;

// Accounting Milestone B against SQL Server: vouchers and the lines they stand for, customers' and suppliers' open items,
// planned, manual and automatic allocations, the cash guard, cashboxes and banks, party opening balances, aging, party
// statements, the subledger reconciliation, bank reconciliation and the permission of each voucher type.
[Collection(DatabaseCollection.Name)]
public sealed class AccountingSubledgerApiTests(DatabaseFixture fx) : IAsyncLifetime
{
    private ApiFactory _api = null!;

    public Task InitializeAsync()
    {
        _api = new ApiFactory(fx.ConnectionString, clock: fx.Clock);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    private HttpClient Admin() => _api.CreateClient().As(AllPermissions, userId: Guid.NewGuid());

    private HttpClient Client(params string[] permissions) => _api.CreateClient().As(string.Join(',', permissions), userId: Guid.NewGuid());

    private async Task<Guid> BranchAsync() => (await fx.SeedBranchAndUnitAsync()).BranchId;

    private Task<Guid> CustomerAsync() => SalesTesting.CreateCustomerAsync(_api.CreateClient().As(SalesTesting.AllPermissions));

    private Task<Guid> SupplierAsync() => PurchasingTesting.CreateSupplierAsync(_api.CreateClient().As(PurchasingTesting.AllPermissions));

    private static DateOnly April(int day) => new(2027, 4, day);

    [Fact]
    public async Task A_receipt_with_tax_withheld_settles_the_invoice_it_names_and_the_rest_is_allocated_by_hand_or_automatically()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var customer = await CustomerAsync();
        var supplier = await SupplierAsync();
        var revenue = await CreateAccountAsync(admin, chart, "41", AccountType.Revenue, StatementSection.Revenue);
        var (bank, bankAccount) = await CreateCashAccountAsync(admin, chart, branch, CashAccountKind.Bank);
        var (cashbox, _) = await CreateCashAccountAsync(admin, chart, branch, CashAccountKind.Cashbox);
        var receivables = chart["120201"];

        // A control-account line names a party of the account's own kind, and one that exists.
        await (await DraftAsync(admin, April(1), PartyLine(receivables, branch, PartyType.Supplier, customer, debit: 1000m), Line(revenue, branch, credit: 1000m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.PartyNotAllowed");
        await (await DraftAsync(admin, April(1), PartyLine(receivables, branch, PartyType.Customer, Guid.NewGuid(), debit: 1000m),
                Line(revenue, branch, credit: 1000m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.PartyNotFound");
        await (await DraftAsync(admin, April(1), Line(revenue, branch, debit: 1m), PartyLine(revenue, branch, PartyType.Customer, customer, credit: 1m)))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.PartyNotAllowed");
        var (invoiceId, _) = await PostJournalAsync(admin, April(1),
            PartyLine(receivables, branch, PartyType.Customer, customer, debit: 1000m, dueDate: April(30)), Line(revenue, branch, credit: 1000m));
        var invoice = ItemOf(await OpenItemsAsync(admin, PartyType.Customer, customer), invoiceId);
        Assert.Equal((1000m, 1000m, "2027-04-30", "Manual"), (invoice.Decimal("amount"), invoice.Decimal("openAmount"), invoice.Text("dueDate"),
            invoice.Text("journalType")));

        // How money moves must fit the cash account; a cheque needs its number; allocations stay within the item and the voucher.
        await (await VoucherDraftAsync(admin, JournalType.CustomerReceipt, April(5), 570m, cashbox, partyType: PartyType.Customer, partyId: customer,
                method: PaymentMethod.BankTransfer))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.MethodDoesNotFitCash");
        await (await VoucherDraftAsync(admin, JournalType.CustomerReceipt, April(5), 570m, bank, partyType: PartyType.Customer, partyId: customer,
                method: PaymentMethod.Cheque))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.ChequeNumberRequired");
        await (await VoucherDraftAsync(admin, JournalType.CustomerReceipt, April(5), 570m, bank, partyType: PartyType.Supplier, partyId: supplier,
                method: PaymentMethod.BankTransfer))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.PartyTypeMismatch");
        await (await VoucherDraftAsync(admin, JournalType.CustomerReceipt, April(5), 1170m, bank, partyType: PartyType.Customer, partyId: customer,
                method: PaymentMethod.BankTransfer, withholding: 30m, allocations: [Allocate(IdOf(invoice), 1100m)]))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.AllocationExceedsOpen");
        await (await VoucherDraftAsync(admin, JournalType.CustomerReceipt, April(5), 570m, bank, partyType: PartyType.Customer, partyId: customer,
                method: PaymentMethod.BankTransfer, withholding: 30m, allocations: [Allocate(IdOf(invoice), 700m)]))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.AllocationsExceedAmount");

        var receiptId = await VoucherDraftAsync(admin, JournalType.CustomerReceipt, April(5), 500m, bank, partyType: PartyType.Customer,
            partyId: customer, method: PaymentMethod.BankTransfer, withholding: 30m, reference: "TR-1", allocations: [Allocate(IdOf(invoice), 530m)])
            .CreatedIdAsync();
        var draft = await JournalAsync(admin, receiptId);
        Assert.True(draft.GetProperty("linesFixed").GetBoolean());
        await (await admin.PutJsonAsync($"{Journals}/{receiptId}", new
            {
                postingDate = Day(April(5)), description = "x", reference = "TR-1", lines = new[] { Line(revenue, branch, debit: 1m), Line(revenue, branch, credit: 1m) },
                rowVersion = draft.Text("rowVersion"),
            }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.VoucherManaged");
        var replaced = await admin.PutJsonAsync($"{Vouchers}/{receiptId}", new
        {
            postingDate = Day(April(5)), description = "تحصيل فاتورة", reference = "TR-1", cashAccountId = bank, counterCashAccountId = (Guid?)null,
            partyType = PartyType.Customer, partyId = customer, paymentMethod = PaymentMethod.BankTransfer, amount = 570m, withholdingAmount = 30m,
            taxAmount = 0m, dueDate = (string?)null, branchId = (Guid?)null, lines = (object[]?)null, allocations = new[] { Allocate(IdOf(invoice), 600m) },
            rowVersion = draft.Text("rowVersion"),
        });
        Assert.True(replaced.StatusCode == HttpStatusCode.OK, await replaced.Content.ReadAsStringAsync());

        // Dr bank 570 (net), Dr withholding tax receivable 30, Cr the customer 600 (gross).
        draft = await JournalAsync(admin, receiptId);
        Assert.Equal([(bankAccount, 570m, 0m, null), (chart["120302"], 30m, 0m, null), (receivables, 0m, 600m, customer)], LinesOf(draft));
        var voucher = draft.GetProperty("voucher");
        Assert.Equal((600m, 30m, "BankTransfer", 600m), (voucher.Decimal("amount"), voucher.Decimal("withholdingAmount"), voucher.Text("paymentMethod"),
            voucher.GetProperty("allocations")[0].Decimal("amount")));
        Assert.NotNull(voucher.Text("partyCode"));

        var posted = await PostNowAsync(admin, receiptId);
        Assert.Matches(@"^RV-2027-\d{6}$", posted.Text("number"));
        var items = await OpenItemsAsync(admin, PartyType.Customer, customer);
        invoice = ItemOf(items, invoiceId);
        var receipt = ItemOf(items, receiptId);
        Assert.Equal((400m, -600m, 0m), (invoice.Decimal("openAmount"), receipt.Decimal("amount"), receipt.Decimal("openAmount")));
        var planned = Assert.Single(invoice.GetProperty("allocations").EnumerateArray());
        Assert.Equal((IdOf(receipt), 600m, receiptId), (planned.GetProperty("otherItemId").GetGuid(), planned.Decimal("amount"),
            planned.GetProperty("journalId").GetGuid()));

        // An unallocated cash receipt; automatic allocation matches it to what is still open, oldest first.
        var cashReceiptId = await PostVoucherAsync(admin, JournalType.CustomerReceipt, April(8), 500m, cashbox, partyType: PartyType.Customer,
            partyId: customer, method: PaymentMethod.Cash);
        var auto = await (await admin.PostJsonAsync($"{Allocations}/auto", new { partyType = PartyType.Customer, partyId = customer })).ReadAsync<JsonElement>();
        Assert.Equal((1, 400m), (auto.GetProperty("allocationsMade").GetInt32(), auto.Decimal("amountAllocated")));
        items = await OpenItemsAsync(admin, PartyType.Customer, customer);
        Assert.Equal((0m, -100m), (ItemOf(items, invoiceId).Decimal("openAmount"), ItemOf(items, cashReceiptId).Decimal("openAmount")));
        Assert.Equal([cashReceiptId], (await OpenItemsAsync(admin, PartyType.Customer, customer, openOnly: true)).Select(i => i.GetProperty("journalId").GetGuid()));

        // Removing an allocation reopens both items; it is removed once. By hand: debit item first, within what is open.
        var automatic = ItemOf(items, invoiceId).GetProperty("allocations").EnumerateArray()
            .Single(a => a.GetProperty("journalId").ValueKind == JsonValueKind.Null).GetProperty("allocationId").GetGuid();
        await admin.PostAsync($"{Allocations}/{automatic}/remove", null).AssertNoContentAsync();
        await (await admin.PostAsync($"{Allocations}/{automatic}/remove", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Allocation.AlreadyRemoved");
        items = await OpenItemsAsync(admin, PartyType.Customer, customer);
        var cashItem = ItemOf(items, cashReceiptId);
        Assert.Equal((400m, -500m), (ItemOf(items, invoiceId).Decimal("openAmount"), cashItem.Decimal("openAmount")));
        await (await admin.PostJsonAsync(Allocations, new { debitItemId = IdOf(invoice), creditItemId = IdOf(cashItem), amount = 450m }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Allocation.ExceedsOpen");
        await (await admin.PostJsonAsync(Allocations, new { debitItemId = IdOf(cashItem), creditItemId = IdOf(invoice), amount = 100m }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Allocation.SidesInvalid");
        await admin.PostJsonAsync(Allocations, new { debitItemId = IdOf(invoice), creditItemId = IdOf(cashItem), amount = 400m }).CreatedIdAsync();

        var statement = await (await admin.GetAsync(
                $"{Reports}/party-statement?partyType=Customer&partyId={customer}&fromDate=2027-04-01&toDate=2027-04-30")).ReadAsync<JsonElement>();
        Assert.Equal((0m, 1000m, 1100m, -100m, 3), (statement.Decimal("openingBalance"), statement.Decimal("totalDebit"), statement.Decimal("totalCredit"),
            statement.Decimal("closingBalance"), statement.GetProperty("lines").GetArrayLength()));
    }

    [Fact]
    public async Task An_expense_bill_with_VAT_is_due_on_the_supplier_terms_and_a_payment_with_tax_withheld_settles_it_from_a_funded_bank()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var supplier = await SupplierAsync();
        var rent = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var capital = await CreateAccountAsync(admin, chart, "3", AccountType.Equity, StatementSection.Equity);
        var (bank, bankAccount) = await CreateCashAccountAsync(admin, chart, branch, CashAccountKind.Bank);
        var payables = chart["220101"];
        object[] rentLine = [VoucherLine(rent, branch, 1000m)];

        await (await VoucherDraftAsync(admin, JournalType.ExpenseBill, April(3), partyType: PartyType.Supplier, partyId: supplier, tax: 140m, lines: rentLine))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.BillNumberRequired");
        await (await VoucherDraftAsync(admin, JournalType.ExpenseBill, April(3), cashAccountId: bank, partyType: PartyType.Supplier, partyId: supplier,
                reference: "INV-1", lines: rentLine))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.CashNotAllowed");
        await (await VoucherDraftAsync(admin, JournalType.ExpenseBill, April(3), partyType: PartyType.Supplier, partyId: supplier, reference: "INV-1",
                lines: [VoucherLine(payables, branch, 1000m)]))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.CounterAccountInvalid");
        await (await VoucherDraftAsync(admin, JournalType.ExpenseBill, April(3), partyType: PartyType.Supplier, partyId: supplier, reference: "INV-1",
                lines: [VoucherLine(bankAccount, branch, 1000m)]))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.CounterAccountInvalid");

        var billId = await VoucherDraftAsync(admin, JournalType.ExpenseBill, April(3), partyType: PartyType.Supplier, partyId: supplier, tax: 140m,
            reference: "INV-1", lines: rentLine).CreatedIdAsync();
        await (await VoucherDraftAsync(admin, JournalType.ExpenseBill, April(4), partyType: PartyType.Supplier, partyId: supplier, reference: "INV-1",
                lines: rentLine))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.Voucher.BillNumberTaken");

        // Dr rent 1000, Dr input VAT 140, Cr the supplier 1140 due after its 30 days of payment terms.
        var bill = await JournalAsync(admin, billId);
        Assert.Equal([(rent, 1000m, 0m, null), (chart["120301"], 140m, 0m, null), (payables, 0m, 1140m, supplier)], LinesOf(bill));
        Assert.Equal(("2027-05-03", "2027-05-03", 1140m), (bill.GetProperty("voucher").Text("dueDate"), bill.GetProperty("lines")[2].Text("dueDate"),
            bill.GetProperty("voucher").Decimal("amount")));
        await PostNowAsync(admin, billId);
        var billItem = ItemOf(await OpenItemsAsync(admin, PartyType.Supplier, supplier), billId);
        Assert.Equal((-1140m, -1140m, "2027-05-03"), (billItem.Decimal("amount"), billItem.Decimal("openAmount"), billItem.Text("dueDate")));

        // A bank without overdraft never pays more than it holds: the payment waits until money comes in.
        var paymentId = await VoucherDraftAsync(admin, JournalType.SupplierPayment, April(10), 1100m, bank, partyType: PartyType.Supplier,
            partyId: supplier, method: PaymentMethod.BankTransfer, withholding: 40m, reference: "TR-9", allocations: [Allocate(IdOf(billItem), 1140m)])
            .CreatedIdAsync();
        var (action, rowVersion) = await ReadyToPostAsync(admin, paymentId);
        await (await ActAsync(admin, paymentId, action, rowVersion, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.CashAccountOverdrawn");
        await PostVoucherAsync(admin, JournalType.CashReceipt, April(1), cashAccountId: bank, lines: [VoucherLine(capital, branch, 5000m)]);
        await PostedAsync(ActAsync(admin, paymentId, action, rowVersion, Guid.NewGuid()));

        // Dr the supplier 1140 (gross), Cr bank 1100 (net), Cr withholding tax payable 40.
        Assert.Equal([(payables, 1140m, 0m, supplier), (bankAccount, 0m, 1100m, null), (chart["220203"], 0m, 40m, null)],
            LinesOf(await JournalAsync(admin, paymentId)));
        var items = await OpenItemsAsync(admin, PartyType.Supplier, supplier);
        Assert.Equal((0m, 1140m, 0m), (ItemOf(items, billId).Decimal("openAmount"), ItemOf(items, paymentId).Decimal("amount"),
            ItemOf(items, paymentId).Decimal("openAmount")));
        Assert.Empty(await OpenItemsAsync(admin, PartyType.Supplier, supplier, openOnly: true));
        Assert.Equal(3900m, (await (await admin.GetAsync($"{CashAccounts}/{bank}")).ReadAsync<JsonElement>()).Decimal("balance"));

        var statement = await (await admin.GetAsync(
                $"{Reports}/party-statement?partyType=Supplier&partyId={supplier}&fromDate=2027-04-01&toDate=2027-04-30")).ReadAsync<JsonElement>();
        Assert.Equal((1140m, 1140m, 0m), (statement.Decimal("totalDebit"), statement.Decimal("totalCredit"), statement.Decimal("closingBalance")));
    }

    [Fact]
    public async Task A_cashbox_never_goes_below_zero_a_bank_only_with_an_overdraft_and_a_transfer_moves_money_between_branches()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var (north, south) = (await BranchAsync(), await BranchAsync());
        var supplies = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var (bank, bankAccount) = await CreateCashAccountAsync(admin, chart, north, CashAccountKind.Bank, allowsOverdraft: true);
        var (cashbox, cashboxAccount) = await CreateCashAccountAsync(admin, chart, south, CashAccountKind.Cashbox);

        // The guard holds for vouchers and hand journals alike, on the voucher's date and every later day.
        var paymentId = await VoucherDraftAsync(admin, JournalType.CashPayment, April(2), cashAccountId: cashbox, lines: [VoucherLine(supplies, south, 100m)])
            .CreatedIdAsync();
        var (action, rowVersion) = await ReadyToPostAsync(admin, paymentId);
        await (await ActAsync(admin, paymentId, action, rowVersion, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.CashAccountOverdrawn");
        var manualId = await DraftAsync(admin, April(2), Line(supplies, south, debit: 50m), Line(cashboxAccount, south, credit: 50m)).CreatedIdAsync();
        var (manualAction, manualVersion) = await ReadyToPostAsync(admin, manualId);
        await (await ActAsync(admin, manualId, manualAction, manualVersion, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.CashAccountOverdrawn");

        await (await VoucherDraftAsync(admin, JournalType.CashTransfer, April(3), 300m, bank))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.CounterCashRequired");
        await (await VoucherDraftAsync(admin, JournalType.CashTransfer, April(3), 300m, bank, bank))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.SameCashAccount");
        await (await VoucherDraftAsync(admin, JournalType.CashTransfer, April(3), 300m, bank, cashbox, method: PaymentMethod.Cash))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.MethodNotAllowed");

        // Dr the cashbox (south), Cr the bank (north); the bank may go below zero, it allows an overdraft.
        var transferId = await PostVoucherAsync(admin, JournalType.CashTransfer, April(3), 300m, bank, cashbox);
        var transfer = await JournalAsync(admin, transferId);
        Assert.Equal([(cashboxAccount, 300m, 0m, null), (bankAccount, 0m, 300m, null)], LinesOf(transfer));
        Assert.Equal([south, north], transfer.GetProperty("lines").EnumerateArray().Select(l => l.GetProperty("branchId").GetGuid()));

        // The cashbox held nothing on 2 April, whatever came in later: the payment is moved to after the transfer.
        await (await ActAsync(admin, paymentId, action, rowVersion, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.CashAccountOverdrawn");
        var moved = await admin.PutJsonAsync($"{Vouchers}/{paymentId}", new
        {
            postingDate = Day(April(4)), description = "أدوات مكتبية", reference = (string?)null, cashAccountId = cashbox,
            counterCashAccountId = (Guid?)null, partyType = (PartyType?)null, partyId = (Guid?)null, paymentMethod = (PaymentMethod?)null, amount = 0m,
            withholdingAmount = 0m, taxAmount = 0m, dueDate = (string?)null, branchId = (Guid?)null, lines = new[] { VoucherLine(supplies, south, 100m) },
            allocations = (object[]?)null, rowVersion,
        });
        Assert.True(moved.StatusCode == HttpStatusCode.OK, await moved.Content.ReadAsStringAsync());
        await PostNowAsync(admin, paymentId);

        Assert.Equal(-300m, (await (await admin.GetAsync($"{CashAccounts}/{bank}")).ReadAsync<JsonElement>()).Decimal("balance"));
        var cashboxes = await (await admin.GetAsync($"{CashAccounts}?kind=Cashbox&branchId={south}")).ReadAsync<JsonElement>();
        var listed = Assert.Single(cashboxes.EnumerateArray());
        Assert.Equal((cashbox, 200m), (IdOf(listed), listed.Decimal("balance")));
        var asOf = await (await admin.GetAsync($"{CashAccounts}?kind=Cashbox&branchId={south}&asOfDate=2027-04-03")).ReadAsync<JsonElement>();
        Assert.Equal(300m, Assert.Single(asOf.EnumerateArray()).Decimal("balance"));

        // One cash account per ledger account and per code; a cashbox has no bank details; a control account holds no money.
        var code = (await (await admin.GetAsync($"{CashAccounts}/{cashbox}")).ReadAsync<JsonElement>()).Text("code");
        var spare = await CreateAccountAsync(admin, chart, "1204", AccountType.Asset, StatementSection.CurrentAssets);
        await (await admin.PostJsonAsync(CashAccounts, new { code, name = "x", kind = CashAccountKind.Cashbox, branchId = south, accountId = spare }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.CashAccount.CodeTaken");
        await (await admin.PostJsonAsync(CashAccounts, new { code = DatabaseFixture.Code("CB"), name = "x", kind = CashAccountKind.Cashbox, branchId = south,
                accountId = cashboxAccount }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.CashAccount.AccountInUse");
        await (await admin.PostJsonAsync(CashAccounts, new { code = DatabaseFixture.Code("CB"), name = "x", kind = CashAccountKind.Cashbox, branchId = south,
                accountId = spare, bankName = "بنك" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.CashAccount.BankDetailsOnCashbox");
        await (await admin.PostJsonAsync(CashAccounts, new { code = DatabaseFixture.Code("CB"), name = "x", kind = CashAccountKind.Cashbox, branchId = south,
                accountId = chart["120201"] }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.CashAccount.AccountInvalid");

        // A cash account with money in it stays active; an empty one is closed and takes no vouchers.
        await (await admin.PostAsync($"{CashAccounts}/{cashbox}/deactivate", null))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.CashAccount.HasBalance");
        var (empty, _) = await CreateCashAccountAsync(admin, chart, south, CashAccountKind.Cashbox);
        await admin.PostAsync($"{CashAccounts}/{empty}/deactivate", null).AssertNoContentAsync();
        await (await VoucherDraftAsync(admin, JournalType.CashTransfer, April(4), 50m, cashbox, empty))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Voucher.CashAccountNotActive");
    }

    [Fact]
    public async Task A_settled_receipt_is_reversed_only_once_its_allocations_are_removed_and_its_reversal_settles_its_item()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var customer = await CustomerAsync();
        var revenue = await CreateAccountAsync(admin, chart, "41", AccountType.Revenue, StatementSection.Revenue);
        var (bank, _) = await CreateCashAccountAsync(admin, chart, branch, CashAccountKind.Bank);
        var receivables = chart["120201"];

        var (invoiceId, _) = await PostJournalAsync(admin, April(1), PartyLine(receivables, branch, PartyType.Customer, customer, debit: 800m),
            Line(revenue, branch, credit: 800m));
        var invoice = ItemOf(await OpenItemsAsync(admin, PartyType.Customer, customer), invoiceId);
        var receiptId = await PostVoucherAsync(admin, JournalType.CustomerReceipt, April(4), 800m, bank, partyType: PartyType.Customer, partyId: customer,
            method: PaymentMethod.Card, allocations: [Allocate(IdOf(invoice), 800m)]);

        await (await admin.PostJsonAsync($"{Journals}/{receiptId}/reverse", new { postingDate = "2027-04-20" }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.ItemsAllocated");

        var allocation = ItemOf(await OpenItemsAsync(admin, PartyType.Customer, customer), invoiceId).GetProperty("allocations")[0]
            .GetProperty("allocationId").GetGuid();
        await admin.PostAsync($"{Allocations}/{allocation}/remove", null).AssertNoContentAsync();
        var reversalId = await admin.PostJsonAsync($"{Journals}/{receiptId}/reverse", new { postingDate = "2027-04-20" }).CreatedIdAsync();

        // Allocated again while the reversal waits: posting checks once more, under the party's lock.
        var again = await admin.PostJsonAsync(Allocations, new { debitItemId = IdOf(invoice), creditItemId = IdOf(ItemOf(
            await OpenItemsAsync(admin, PartyType.Customer, customer), receiptId)), amount = 100m }).CreatedIdAsync();
        var (action, rowVersion) = await ReadyToPostAsync(admin, reversalId);
        await (await ActAsync(admin, reversalId, action, rowVersion, Guid.NewGuid()))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.Journal.ItemsAllocated");
        await admin.PostAsync($"{Allocations}/{again}/remove", null).AssertNoContentAsync();
        await PostedAsync(ActAsync(admin, reversalId, action, rowVersion, Guid.NewGuid()));

        // The reversal's item (a debit on the customer) settles the receipt's item; the invoice is open again.
        var items = await OpenItemsAsync(admin, PartyType.Customer, customer);
        var receipt = ItemOf(items, receiptId);
        var reversal = ItemOf(items, reversalId);
        Assert.Equal((800m, 0m, 0m, 800m), (ItemOf(items, invoiceId).Decimal("openAmount"), receipt.Decimal("openAmount"), reversal.Decimal("openAmount"),
            reversal.Decimal("amount")));
        var settled = Assert.Single(reversal.GetProperty("allocations").EnumerateArray());
        Assert.Equal((IdOf(receipt), reversalId), (settled.GetProperty("otherItemId").GetGuid(), settled.GetProperty("journalId").GetGuid()));
        Assert.Equal(reversalId, (await JournalAsync(admin, receiptId)).GetProperty("reversedByJournalId").GetGuid());
        Assert.Equal(0m, (await (await admin.GetAsync($"{CashAccounts}/{bank}")).ReadAsync<JsonElement>()).Decimal("balance"));
    }

    [Fact]
    public async Task Party_opening_balances_age_by_due_date_and_the_subledgers_reconcile_with_their_control_accounts()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var (branch, other) = (await BranchAsync(), await BranchAsync());
        var customer = await CustomerAsync();
        var supplier = await SupplierAsync();
        var (bank, _) = await CreateCashAccountAsync(admin, chart, branch, CashAccountKind.Bank);
        var (receivables, payables) = (chart["120201"], chart["220101"]);

        var openingId = await admin.PostJsonAsync($"{Journals}/opening", new
        {
            description = "أرصدة العملاء والموردين", reference = "OB-P",
            lines = new[]
            {
                PartyLine(receivables, branch, PartyType.Customer, customer, debit: 2500m, dueDate: new DateOnly(2026, 11, 20)),
                PartyLine(receivables, branch, PartyType.Customer, customer, debit: 700m, dueDate: new DateOnly(2027, 2, 15)),
                PartyLine(payables, branch, PartyType.Supplier, supplier, credit: 900m, dueDate: new DateOnly(2026, 12, 20)),
                Line(chart["35"], branch, credit: 2300m),
            },
        }).CreatedIdAsync();
        await PostNowAsync(admin, openingId);
        await PostVoucherAsync(admin, JournalType.CustomerReceipt, new DateOnly(2027, 1, 20), 200m, bank, partyType: PartyType.Customer, partyId: customer,
            method: PaymentMethod.BankTransfer);

        // As of 31 January: 2500 is 72 days late, 700 not yet due, the receipt not yet allocated; the supplier's 900 is 42 days late.
        var aging = await (await admin.GetAsync($"{Reports}/aging?partyType=Customer&asOfDate=2027-01-31")).ReadAsync<JsonElement>();
        var row = aging.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("partyId").GetGuid() == customer);
        Assert.Equal((700m, 0m, 0m, 2500m, 0m, -200m, 3000m), (row.Decimal("notDue"), row.Decimal("days1To30"), row.Decimal("days31To60"),
            row.Decimal("days61To90"), row.Decimal("over90Days"), row.Decimal("unallocated"), row.Decimal("total")));
        Assert.Equal(aging.GetProperty("rows").EnumerateArray().Sum(r => r.Decimal("total")), aging.Decimal("total"));
        var suppliers = await (await admin.GetAsync($"{Reports}/aging?partyType=Supplier&asOfDate=2027-01-31")).ReadAsync<JsonElement>();
        var supplierRow = suppliers.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("partyId").GetGuid() == supplier);
        Assert.Equal((900m, 900m), (supplierRow.Decimal("days31To60"), supplierRow.Decimal("total")));

        // Later, both customer items have aged; before the receipt it is not there; another branch has none of this.
        var later = await (await admin.GetAsync($"{Reports}/aging?partyType=Customer&asOfDate=2027-03-31")).ReadAsync<JsonElement>();
        row = later.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("partyId").GetGuid() == customer);
        Assert.Equal((0m, 700m, 2500m, -200m), (row.Decimal("notDue"), row.Decimal("days31To60"), row.Decimal("over90Days"), row.Decimal("unallocated")));
        var before = await (await admin.GetAsync($"{Reports}/aging?partyType=Customer&asOfDate=2027-01-10")).ReadAsync<JsonElement>();
        Assert.Equal(0m, before.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("partyId").GetGuid() == customer).Decimal("unallocated"));
        var elsewhere = await (await admin.GetAsync($"{Reports}/aging?partyType=Customer&asOfDate=2027-01-31&branchId={other}")).ReadAsync<JsonElement>();
        Assert.DoesNotContain(elsewhere.GetProperty("rows").EnumerateArray(), r => r.GetProperty("partyId").GetGuid() == customer);

        var statement = await (await admin.GetAsync(
                $"{Reports}/party-statement?partyType=Supplier&partyId={supplier}&fromDate=2027-01-01&toDate=2027-01-31")).ReadAsync<JsonElement>();
        Assert.Equal((0m, 0m, 900m, -900m), (statement.Decimal("openingBalance"), statement.Decimal("totalDebit"), statement.Decimal("totalCredit"),
            statement.Decimal("closingBalance")));

        // Every control account equals what its open items amount to and what is still open of them.
        var reconciliation = await (await admin.GetAsync($"{Reports}/subledger-reconciliation?asOfDate=2027-12-31")).ReadAsync<JsonElement>();
        Assert.True(reconciliation.GetProperty("isReconciled").GetBoolean(), reconciliation.ToString());
        var accounts = reconciliation.GetProperty("accounts").EnumerateArray().ToList();
        Assert.Contains(accounts, a => a.GetProperty("accountId").GetGuid() == receivables);
        Assert.Contains(accounts, a => a.GetProperty("accountId").GetGuid() == payables);
        Assert.All(accounts, a => Assert.Equal((a.Decimal("ledgerBalance"), 0m, 0m),
            (a.Decimal("openItemsOpenAmount"), a.Decimal("difference"), a.Decimal("linesWithoutParty"))));
    }

    [Fact]
    public async Task A_bank_reconciliation_ticks_cleared_lines_until_nothing_differs_and_a_line_is_cleared_by_one_statement_only()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var capital = await CreateAccountAsync(admin, chart, "3", AccountType.Equity, StatementSection.Equity);
        var fees = await CreateAccountAsync(admin, chart, "53", AccountType.Expense, StatementSection.OperatingExpenses);
        var (bank, _) = await CreateCashAccountAsync(admin, chart, branch, CashAccountKind.Bank);
        var (cashbox, _) = await CreateCashAccountAsync(admin, chart, branch, CashAccountKind.Cashbox);

        var depositId = await PostVoucherAsync(admin, JournalType.CashReceipt, new DateOnly(2027, 5, 2), cashAccountId: bank,
            lines: [VoucherLine(capital, branch, 5000m)]);
        var transferId = await PostVoucherAsync(admin, JournalType.CashTransfer, new DateOnly(2027, 5, 5), 300m, bank, cashbox);
        var feeId = await PostVoucherAsync(admin, JournalType.CashPayment, new DateOnly(2027, 5, 20), cashAccountId: bank,
            lines: [VoucherLine(fees, branch, 25m)]);
        var lateId = await PostVoucherAsync(admin, JournalType.CashReceipt, new DateOnly(2027, 6, 3), cashAccountId: bank,
            lines: [VoucherLine(capital, branch, 1000m)]);

        await (await admin.PostJsonAsync(BankReconciliations, new { cashAccountId = cashbox, statementDate = "2027-05-31", statementBalance = 0m }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.BankReconciliation.NotBank");
        var mayId = await admin.PostJsonAsync(BankReconciliations, new { cashAccountId = bank, statementDate = "2027-05-31", statementBalance = 4700m })
            .CreatedIdAsync();
        await (await admin.PostJsonAsync(BankReconciliations, new { cashAccountId = bank, statementDate = "2027-05-31", statementBalance = 1m }))
            .AssertProblemAsync(HttpStatusCode.Conflict, "Accounting.BankReconciliation.DraftExists");

        // Lines up to the statement date only; nothing is ticked yet, so the whole statement balance differs.
        var may = await RecAsync(admin, mayId);
        Assert.Equal([(depositId, 5000m), (transferId, -300m), (feeId, -25m)], BankLines(may).Select(l => (l.JournalId, l.Amount)));
        Assert.Equal((0m, 4700m, 4675m), (may.Decimal("openingClearedBalance"), may.Decimal("difference"), may.Decimal("ledgerBalance")));
        await (await admin.PostJsonAsync($"{BankReconciliations}/{mayId}/complete", new { rowVersion = may.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.BankReconciliation.NotBalanced");

        var cleared = BankLines(may).Where(l => l.JournalId != feeId).Select(l => l.LineId).ToArray();
        var ticked = await admin.PutJsonAsync($"{BankReconciliations}/{mayId}/lines", new { journalLineIds = cleared, rowVersion = may.Text("rowVersion") });
        Assert.True(ticked.StatusCode == HttpStatusCode.OK, await ticked.Content.ReadAsStringAsync());
        may = await RecAsync(admin, mayId);
        Assert.Equal((4700m, 0m), (may.Decimal("clearedAmount"), may.Decimal("difference")));
        Assert.Equal([true, true, false], BankLines(may).Select(l => l.Ticked));
        var completed = await admin.PostJsonAsync($"{BankReconciliations}/{mayId}/complete", new { rowVersion = may.Text("rowVersion") });
        Assert.True(completed.StatusCode == HttpStatusCode.OK, await completed.Content.ReadAsStringAsync());
        may = await RecAsync(admin, mayId);
        Assert.Equal(("Completed", 2), (may.Text("status"), may.GetProperty("lines").GetArrayLength()));

        // The next statement starts from what May cleared, offers only the lines nobody cleared, and comes after May.
        await (await admin.PostJsonAsync(BankReconciliations, new { cashAccountId = bank, statementDate = "2027-05-15", statementBalance = 0m }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.BankReconciliation.BeforeLastStatement");
        var juneId = await admin.PostJsonAsync(BankReconciliations, new { cashAccountId = bank, statementDate = "2027-06-30", statementBalance = 5675m })
            .CreatedIdAsync();
        var june = await RecAsync(admin, juneId);
        Assert.Equal([feeId, lateId], BankLines(june).Select(l => l.JournalId));
        Assert.Equal((4700m, 975m), (june.Decimal("openingClearedBalance"), june.Decimal("difference")));
        await (await admin.PutJsonAsync($"{BankReconciliations}/{juneId}/lines", new { journalLineIds = cleared.Take(1).ToArray(), rowVersion = june.Text("rowVersion") }))
            .AssertProblemAsync(HttpStatusCode.UnprocessableEntity, "Accounting.BankReconciliation.LineNotFree");
        var all = await admin.PutJsonAsync($"{BankReconciliations}/{juneId}/lines", new
        {
            journalLineIds = BankLines(june).Select(l => l.LineId).ToArray(), rowVersion = june.Text("rowVersion"),
        });
        Assert.True(all.StatusCode == HttpStatusCode.OK, await all.Content.ReadAsStringAsync());
        june = await RecAsync(admin, juneId);
        Assert.Equal(0m, june.Decimal("difference"));

        // A cancelled draft frees its lines and lets another reconciliation of the same statement start.
        var cancelled = await admin.PostJsonAsync($"{BankReconciliations}/{juneId}/cancel", new { rowVersion = june.Text("rowVersion") });
        Assert.True(cancelled.StatusCode == HttpStatusCode.OK, await cancelled.Content.ReadAsStringAsync());
        Assert.Equal("Cancelled", (await RecAsync(admin, juneId)).Text("status"));
        await admin.PostJsonAsync(BankReconciliations, new { cashAccountId = bank, statementDate = "2027-06-30", statementBalance = 5675m }).CreatedIdAsync();

        var listed = await (await admin.GetAsync($"{BankReconciliations}?cashAccountId={bank}")).ReadAsync<JsonElement>();
        Assert.Equal(3, listed.GetArrayLength());
    }

    [Fact]
    public async Task Each_voucher_type_and_allocation_needs_the_permission_of_its_party_or_of_the_treasury()
    {
        var admin = Admin();
        var chart = await EnsureLedgerAsync(fx, admin);
        var branch = await BranchAsync();
        var customer = await CustomerAsync();
        var supplier = await SupplierAsync();
        var (bank, _) = await CreateCashAccountAsync(admin, chart, branch, CashAccountKind.Bank, allowsOverdraft: true);
        var (cashbox, _) = await CreateCashAccountAsync(admin, chart, branch, CashAccountKind.Cashbox);
        var receivablesClerk = Client(AccountingPermissions.ReceivablesManage, AccountingPermissions.JournalsRead, AccountingPermissions.ReportsRead);
        var treasurer = Client(AccountingPermissions.TreasuryManage, AccountingPermissions.AccountsRead);

        var receiptId = await VoucherDraftAsync(receivablesClerk, JournalType.CustomerReceipt, April(6), 100m, bank, partyType: PartyType.Customer,
            partyId: customer, method: PaymentMethod.BankTransfer).CreatedIdAsync();
        await PostNowAsync(receivablesClerk, receiptId);
        await (await VoucherDraftAsync(receivablesClerk, JournalType.SupplierPayment, April(6), 100m, bank, partyType: PartyType.Supplier,
                partyId: supplier, method: PaymentMethod.BankTransfer))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Accounting.Access.PermissionDenied");
        await (await VoucherDraftAsync(receivablesClerk, JournalType.CashTransfer, April(6), 50m, bank, cashbox))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Accounting.Access.PermissionDenied");
        await (await receivablesClerk.PostJsonAsync($"{Allocations}/auto", new { partyType = PartyType.Supplier, partyId = supplier }))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Accounting.Access.PermissionDenied");
        Assert.Equal(HttpStatusCode.OK, (await receivablesClerk.PostJsonAsync($"{Allocations}/auto", new { partyType = PartyType.Customer, partyId = customer }))
            .StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await receivablesClerk.GetAsync(BankReconciliations)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await receivablesClerk.PostJsonAsync(CashAccounts, new { code = "X" })).StatusCode);

        await VoucherDraftAsync(treasurer, JournalType.CashTransfer, April(6), 50m, bank, cashbox).CreatedIdAsync();
        await (await VoucherDraftAsync(treasurer, JournalType.CustomerReceipt, April(6), 100m, bank, partyType: PartyType.Customer, partyId: customer,
                method: PaymentMethod.BankTransfer))
            .AssertProblemAsync(HttpStatusCode.Forbidden, "Accounting.Access.PermissionDenied");
        Assert.Equal(HttpStatusCode.OK, (await treasurer.GetAsync(CashAccounts)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await treasurer.GetAsync($"{OpenItems}?partyType=Customer")).StatusCode);
    }

    private static async Task<JsonElement> RecAsync(HttpClient client, Guid id) =>
        await (await client.GetAsync($"{BankReconciliations}/{id}")).ReadAsync<JsonElement>();

    private static List<(Guid LineId, Guid JournalId, decimal Amount, bool Ticked)> BankLines(JsonElement reconciliation) =>
        reconciliation.GetProperty("lines").EnumerateArray()
            .Select(l => (l.GetProperty("journalLineId").GetGuid(), l.GetProperty("journalId").GetGuid(), l.Decimal("amount"), l.GetProperty("isTicked").GetBoolean()))
            .ToList();
}
