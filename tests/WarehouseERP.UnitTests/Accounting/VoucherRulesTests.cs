using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.UnitTests.Accounting;

/// <summary>
/// The fixed line shape of each voucher type, the rules a voucher is checked against before it becomes lines, its number
/// prefixes and the bank reconciliation's arithmetic (Accounting Milestone B).
/// </summary>
public sealed class VoucherRulesTests
{
    private static readonly Guid Branch = Guid.NewGuid();
    private static readonly VoucherCash Bank = new(Guid.NewGuid(), CashAccountKind.Bank, Guid.NewGuid(), Branch);
    private static readonly VoucherCash Cashbox = new(Guid.NewGuid(), CashAccountKind.Cashbox, Guid.NewGuid(), Branch);
    private static readonly VoucherParty Customer = new(PartyType.Customer, Guid.NewGuid(), Guid.NewGuid());
    private static readonly VoucherParty Supplier = new(PartyType.Supplier, Guid.NewGuid(), Guid.NewGuid());
    private static readonly Guid Withholding = Guid.NewGuid();
    private static readonly Guid InputVat = Guid.NewGuid();
    private static readonly Guid Expense = Guid.NewGuid();
    private static readonly DateOnly Day = new(2027, 4, 5);

    private static VoucherInput Input(
        JournalType type, VoucherCash? cash = null, VoucherParty? party = null, PaymentMethod? method = null, decimal amount = 0m,
        decimal withholding = 0m, decimal tax = 0m, string? reference = null, VoucherCash? counter = null, DateOnly? due = null,
        VoucherLineInput[]? lines = null, VoucherAllocationInput[]? allocations = null) =>
        new(type, Day, "سند", reference, "EGP", false, Branch, cash, counter, party, method, amount, withholding,
            withholding > 0m ? Withholding : null, tax, tax > 0m ? InputVat : null, due, lines ?? [], allocations ?? []);

    private static List<(Guid Account, decimal Debit, decimal Credit, Guid? Party)> Lines(VoucherInput input)
    {
        var built = VoucherLines.Build(input);
        Assert.True(built.IsSuccess, built.IsFailure ? built.Error.Code : null);
        return built.Value.Select(l => (l.AccountId, l.Debit, l.Credit, l.PartyId)).ToList();
    }

    [Fact]
    public void Receipts_and_payments_carry_tax_withheld_on_its_own_account_and_settle_the_party_gross()
    {
        var receipt = Input(JournalType.CustomerReceipt, Bank, Customer, PaymentMethod.BankTransfer, amount: 570m, withholding: 30m);
        Assert.Equal([(Bank.AccountId, 570m, 0m, null), (Withholding, 30m, 0m, null), (Customer.ControlAccountId, 0m, 600m, Customer.PartyId)],
            Lines(receipt));
        Assert.Equal(600m, VoucherLines.Total(receipt));

        var payment = Input(JournalType.SupplierPayment, Cashbox, Supplier, PaymentMethod.Cash, amount: 1100m, withholding: 40m);
        Assert.Equal([(Supplier.ControlAccountId, 1140m, 0m, Supplier.PartyId), (Cashbox.AccountId, 0m, 1100m, null), (Withholding, 0m, 40m, null)],
            Lines(payment));

        // All of it withheld: no cash line, the party still settled in full.
        Assert.Equal([(Withholding, 25m, 0m, null), (Customer.ControlAccountId, 0m, 25m, Customer.PartyId)],
            Lines(Input(JournalType.CustomerReceipt, Bank, Customer, PaymentMethod.BankTransfer, withholding: 25m)));
    }

    [Fact]
    public void Refunds_cash_vouchers_transfers_and_bills_have_one_shape_each()
    {
        Assert.Equal([(Customer.ControlAccountId, 50m, 0m, Customer.PartyId), (Bank.AccountId, 0m, 50m, null)],
            Lines(Input(JournalType.CustomerRefund, Bank, Customer, PaymentMethod.Cheque, amount: 50m, reference: "CHQ-7")));
        Assert.Equal([(Cashbox.AccountId, 60m, 0m, null), (Supplier.ControlAccountId, 0m, 60m, Supplier.PartyId)],
            Lines(Input(JournalType.SupplierRefund, Cashbox, Supplier, PaymentMethod.Cash, amount: 60m)));
        Assert.Equal([(Expense, 30m, 0m, null), (Expense, 20m, 0m, null), (Cashbox.AccountId, 0m, 50m, null)],
            Lines(Input(JournalType.CashPayment, Cashbox, lines: [new(Expense, Branch, 30m, null), new(Expense, Branch, 20m, null)])));
        Assert.Equal([(Bank.AccountId, 75m, 0m, null), (Expense, 0m, 75m, null)],
            Lines(Input(JournalType.CashReceipt, Bank, lines: [new(Expense, Branch, 75m, null)])));
        Assert.Equal([(Cashbox.AccountId, 300m, 0m, null), (Bank.AccountId, 0m, 300m, null)],
            Lines(Input(JournalType.CashTransfer, Bank, counter: Cashbox, amount: 300m)));

        var bill = Input(JournalType.ExpenseBill, party: Supplier, tax: 140m, reference: "INV-1", due: Day.AddDays(30),
            lines: [new(Expense, Branch, 1000m, "إيجار")]);
        Assert.Equal([(Expense, 1000m, 0m, null), (InputVat, 140m, 0m, null), (Supplier.ControlAccountId, 0m, 1140m, Supplier.PartyId)], Lines(bill));
        Assert.Equal(Day.AddDays(30), VoucherLines.Build(bill).Value[^1].DueDate);
        Assert.Equal(1140m, VoucherLines.Total(bill));
    }

    [Fact]
    public void A_voucher_is_refused_when_its_cash_party_method_or_amounts_do_not_fit_its_type()
    {
        Assert.Equal(VoucherDomainErrors.CashRequired, VoucherLines.Build(Input(JournalType.CustomerReceipt, party: Customer, amount: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.CashNotAllowed, VoucherLines.Build(Input(JournalType.ExpenseBill, Bank, Supplier, reference: "B",
            due: Day, lines: [new(Expense, Branch, 1m, null)])).Error);
        Assert.Equal(VoucherDomainErrors.SameCashAccount, VoucherLines.Build(Input(JournalType.CashTransfer, Bank, counter: Bank, amount: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.CounterCashNotAllowed,
            VoucherLines.Build(Input(JournalType.CashReceipt, Bank, counter: Cashbox, lines: [new(Expense, Branch, 1m, null)])).Error);
        Assert.Equal(VoucherDomainErrors.PartyRequired, VoucherLines.Build(Input(JournalType.SupplierPayment, Bank, method: PaymentMethod.Card, amount: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.PartyNotAllowed,
            VoucherLines.Build(Input(JournalType.CashPayment, Cashbox, Customer, lines: [new(Expense, Branch, 1m, null)])).Error);
        Assert.Equal(VoucherDomainErrors.PartyTypeMismatch(PartyType.Customer).Code,
            VoucherLines.Build(Input(JournalType.CustomerReceipt, Bank, Supplier, PaymentMethod.BankTransfer, amount: 1m)).Error!.Code);
        Assert.Equal(VoucherDomainErrors.MethodRequired, VoucherLines.Build(Input(JournalType.CustomerReceipt, Bank, Customer, amount: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.MethodDoesNotFitCash,
            VoucherLines.Build(Input(JournalType.CustomerReceipt, Cashbox, Customer, PaymentMethod.Card, amount: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.MethodDoesNotFitCash,
            VoucherLines.Build(Input(JournalType.CustomerReceipt, Bank, Customer, PaymentMethod.Cash, amount: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.ChequeNumberRequired,
            VoucherLines.Build(Input(JournalType.SupplierPayment, Bank, Supplier, PaymentMethod.Cheque, amount: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.MethodNotAllowed,
            VoucherLines.Build(Input(JournalType.CashTransfer, Bank, counter: Cashbox, method: PaymentMethod.BankTransfer, amount: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.WithholdingInvalid,
            VoucherLines.Build(Input(JournalType.CustomerRefund, Bank, Customer, PaymentMethod.Card, amount: 10m, withholding: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.TaxInvalid,
            VoucherLines.Build(Input(JournalType.CustomerReceipt, Bank, Customer, PaymentMethod.Card, amount: 10m, tax: 1m)).Error);
        Assert.Equal(VoucherDomainErrors.AmountInvalid, VoucherLines.Build(Input(JournalType.CustomerReceipt, Bank, Customer, PaymentMethod.Card)).Error);
        Assert.Equal(VoucherDomainErrors.LinesRequired, VoucherLines.Build(Input(JournalType.CashPayment, Cashbox)).Error);
        Assert.Equal(VoucherDomainErrors.LinesNotAllowed, VoucherLines.Build(Input(JournalType.CashTransfer, Bank, counter: Cashbox, amount: 1m,
            lines: [new(Expense, Branch, 1m, null)])).Error);
        Assert.Equal(VoucherDomainErrors.LineAmountInvalid(2).Message, VoucherLines.Build(Input(JournalType.CashReceipt, Bank,
            lines: [new(Expense, Branch, 1m, null), new(Expense, Branch, 0m, null)])).Error!.Message);
        Assert.Equal(VoucherDomainErrors.BillNumberRequired, VoucherLines.Build(Input(JournalType.ExpenseBill, party: Supplier, due: Day,
            lines: [new(Expense, Branch, 1m, null)])).Error);
        Assert.Equal(VoucherDomainErrors.DueBeforeBill, VoucherLines.Build(Input(JournalType.ExpenseBill, party: Supplier, reference: "B",
            due: Day.AddDays(-1), lines: [new(Expense, Branch, 1m, null)])).Error);
    }

    [Fact]
    public void Planned_allocations_belong_to_settling_vouchers_name_each_item_once_and_stay_within_the_gross_amount()
    {
        var item = Guid.NewGuid();
        Assert.Equal(VoucherDomainErrors.AllocationsNotAllowed, VoucherLines.Build(Input(JournalType.CashReceipt, Bank,
            lines: [new(Expense, Branch, 5m, null)], allocations: [new(item, 5m)])).Error);
        Assert.Equal(VoucherDomainErrors.AllocationInvalid, VoucherLines.Build(Input(JournalType.CustomerReceipt, Bank, Customer, PaymentMethod.Card,
            amount: 10m, allocations: [new(item, 4m), new(item, 4m)])).Error);
        Assert.Equal(VoucherDomainErrors.AllocationInvalid, VoucherLines.Build(Input(JournalType.CustomerReceipt, Bank, Customer, PaymentMethod.Card,
            amount: 10m, allocations: [new(item, 0m)])).Error);
        Assert.Equal(VoucherDomainErrors.AllocationsExceedAmount, VoucherLines.Build(Input(JournalType.CustomerReceipt, Bank, Customer, PaymentMethod.Card,
            amount: 10m, withholding: 1m, allocations: [new(item, 11.01m)])).Error);
        Assert.True(VoucherLines.Build(Input(JournalType.CustomerReceipt, Bank, Customer, PaymentMethod.Card, amount: 10m, withholding: 1m,
            allocations: [new(item, 11m)])).IsSuccess);
    }

    [Fact]
    public void Every_voucher_type_numbers_under_its_own_prefix()
    {
        Assert.Equal("RV-2027-000001", JournalNumbers.Format(JournalType.CustomerReceipt, 2027, 1));
        Assert.Equal(["PV", "CF", "SF", "CR", "CP", "TR", "EB"],
            new[] { JournalType.SupplierPayment, JournalType.CustomerRefund, JournalType.SupplierRefund, JournalType.CashReceipt, JournalType.CashPayment,
                JournalType.CashTransfer, JournalType.ExpenseBill }.Select(JournalNumbers.Prefix));
        Assert.Equal(Enum.GetValues<JournalType>().Length, Enum.GetValues<JournalType>().Select(JournalNumbers.Prefix).Distinct().Count());
    }

    [Fact]
    public void A_bank_reconciliation_completes_only_when_the_statement_equals_what_was_cleared_before_and_now()
    {
        var reconciliation = BankReconciliation.Start(Guid.NewGuid(), new DateOnly(2027, 5, 31), 1000m);
        var (deposit, fee) = (Guid.NewGuid(), Guid.NewGuid());
        Assert.True(reconciliation.Tick([(deposit, 700m), (fee, -50m)]).IsSuccess);
        Assert.Equal((650m, 50m), (reconciliation.ClearedAmount, reconciliation.Difference(openingClearedBalance: 300m)));
        Assert.Equal(BankReconciliationDomainErrors.NotBalanced(50m).Message, reconciliation.Complete(300m, null, DateTime.UtcNow).Error!.Message);

        Assert.True(reconciliation.Tick([(deposit, 700m)]).IsSuccess);
        Assert.Equal(0m, reconciliation.Difference(300m));
        Assert.True(reconciliation.Complete(300m, null, DateTime.UtcNow).IsSuccess);
        Assert.Equal(BankReconciliationStatus.Completed, reconciliation.Status);
        Assert.Equal(BankReconciliationDomainErrors.NotDraft, reconciliation.Tick([]).Error);
        Assert.Equal(BankReconciliationDomainErrors.NotDraft, reconciliation.Cancel().Error);
    }
}
