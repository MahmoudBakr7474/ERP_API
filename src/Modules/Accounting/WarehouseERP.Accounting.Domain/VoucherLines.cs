using WarehouseERP.Accounting.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// The journal lines a voucher stands for. Each type has one fixed shape, so a voucher always balances by construction:
/// <list type="bullet">
/// <item>customer receipt: Dr cash (net), Dr withholding receivable (tax withheld), Cr receivables (gross, the customer);</item>
/// <item>supplier payment: Dr payables (gross, the supplier), Cr cash (net), Cr withholding payable (tax withheld);</item>
/// <item>customer refund: Dr receivables (the customer), Cr cash; supplier refund: Dr cash, Cr payables (the supplier);</item>
/// <item>cash receipt: Dr cash, Cr each counter line; cash payment: Dr each counter line, Cr cash;</item>
/// <item>transfer: Dr destination, Cr source (each in its own branch);</item>
/// <item>expense bill: Dr each expense line, Dr input VAT, Cr payables (total, the supplier, its due date).</item>
/// </list>
/// Cash goes through a cashbox only as cash, through a bank only by transfer, cheque or card; a cheque is recorded only once
/// it has cleared and needs its number.
/// </summary>
public static class VoucherLines
{
    public const int MaxLines = 200;

    /// <summary>The party a voucher type names, if any.</summary>
    public static PartyType? PartyTypeOf(JournalType type) => type switch
    {
        JournalType.CustomerReceipt or JournalType.CustomerRefund => PartyType.Customer,
        JournalType.SupplierPayment or JournalType.SupplierRefund or JournalType.ExpenseBill => PartyType.Supplier,
        _ => null,
    };

    /// <summary>The voucher's total as kept on its header.</summary>
    public static decimal Total(VoucherInput input) => input.Type switch
    {
        JournalType.CustomerReceipt or JournalType.SupplierPayment => input.Amount + input.WithholdingAmount,
        JournalType.CashReceipt or JournalType.CashPayment => input.Lines.Sum(l => l.Amount),
        JournalType.ExpenseBill => input.Lines.Sum(l => l.Amount) + input.TaxAmount,
        _ => input.Amount,
    };

    public static Result<IReadOnlyList<JournalLineInput>> Build(VoucherInput input)
    {
        var checkedInput = Check(input);
        if (checkedInput.IsFailure)
            return checkedInput.Error;

        var lines = new List<JournalLineInput>();
        var cash = input.Cash;
        var party = input.Party;
        var gross = Total(input);
        JournalLineInput Party(decimal debit, decimal credit, DateOnly? due = null) =>
            new(party!.ControlAccountId, input.BranchId, debit, credit, null, party.Type, party.PartyId, due ?? input.PostingDate);

        switch (input.Type)
        {
            case JournalType.CustomerReceipt:
                if (input.Amount > 0m)
                    lines.Add(new JournalLineInput(cash!.AccountId, cash.BranchId, input.Amount, 0m, null));
                if (input.WithholdingAmount > 0m)
                    lines.Add(new JournalLineInput(input.WithholdingAccountId!.Value, input.BranchId, input.WithholdingAmount, 0m, null));
                lines.Add(Party(0m, gross));
                break;
            case JournalType.SupplierPayment:
                lines.Add(Party(gross, 0m));
                if (input.Amount > 0m)
                    lines.Add(new JournalLineInput(cash!.AccountId, cash.BranchId, 0m, input.Amount, null));
                if (input.WithholdingAmount > 0m)
                    lines.Add(new JournalLineInput(input.WithholdingAccountId!.Value, input.BranchId, 0m, input.WithholdingAmount, null));
                break;
            case JournalType.CustomerRefund:
                lines.Add(Party(input.Amount, 0m));
                lines.Add(new JournalLineInput(cash!.AccountId, cash.BranchId, 0m, input.Amount, null));
                break;
            case JournalType.SupplierRefund:
                lines.Add(new JournalLineInput(cash!.AccountId, cash.BranchId, input.Amount, 0m, null));
                lines.Add(Party(0m, input.Amount));
                break;
            case JournalType.CashReceipt:
                lines.Add(new JournalLineInput(cash!.AccountId, cash.BranchId, gross, 0m, null));
                lines.AddRange(input.Lines.Select(l => new JournalLineInput(l.AccountId, l.BranchId, 0m, l.Amount, l.Description)));
                break;
            case JournalType.CashPayment:
                lines.AddRange(input.Lines.Select(l => new JournalLineInput(l.AccountId, l.BranchId, l.Amount, 0m, l.Description)));
                lines.Add(new JournalLineInput(cash!.AccountId, cash.BranchId, 0m, gross, null));
                break;
            case JournalType.CashTransfer:
                lines.Add(new JournalLineInput(input.CounterCash!.AccountId, input.CounterCash.BranchId, input.Amount, 0m, null));
                lines.Add(new JournalLineInput(cash!.AccountId, cash.BranchId, 0m, input.Amount, null));
                break;
            case JournalType.ExpenseBill:
                lines.AddRange(input.Lines.Select(l => new JournalLineInput(l.AccountId, l.BranchId, l.Amount, 0m, l.Description)));
                if (input.TaxAmount > 0m)
                    lines.Add(new JournalLineInput(input.TaxAccountId!.Value, input.BranchId, input.TaxAmount, 0m, null));
                lines.Add(Party(0m, gross, input.DueDate));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(input), input.Type, "Not a voucher type.");
        }
        return Result.Success<IReadOnlyList<JournalLineInput>>(lines);
    }

    private static Result Check(VoucherInput input)
    {
        var type = input.Type;
        var withLines = type is JournalType.CashReceipt or JournalType.CashPayment or JournalType.ExpenseBill;
        var settlesParty = type is JournalType.CustomerReceipt or JournalType.SupplierPayment or JournalType.CustomerRefund
            or JournalType.SupplierRefund;

        if (type == JournalType.ExpenseBill ? input.Cash is not null : input.Cash is null)
            return type == JournalType.ExpenseBill ? VoucherDomainErrors.CashNotAllowed : VoucherDomainErrors.CashRequired;
        if (type == JournalType.CashTransfer)
        {
            if (input.CounterCash is null)
                return VoucherDomainErrors.CounterCashRequired;
            if (input.CounterCash.CashAccountId == input.Cash!.CashAccountId)
                return VoucherDomainErrors.SameCashAccount;
        }
        else if (input.CounterCash is not null)
            return VoucherDomainErrors.CounterCashNotAllowed;

        var partyType = PartyTypeOf(type);
        if (partyType is null ? input.Party is not null : input.Party is null)
            return partyType is null ? VoucherDomainErrors.PartyNotAllowed : VoucherDomainErrors.PartyRequired;
        if (partyType is not null && input.Party!.Type != partyType)
            return VoucherDomainErrors.PartyTypeMismatch(partyType.Value);

        if (settlesParty)
        {
            if (input.PaymentMethod is not { } method || !Enum.IsDefined(method))
                return VoucherDomainErrors.MethodRequired;
            var fits = input.Cash!.Kind == CashAccountKind.Cashbox ? method == PaymentMethod.Cash : method != PaymentMethod.Cash;
            if (!fits)
                return VoucherDomainErrors.MethodDoesNotFitCash;
            if (method == PaymentMethod.Cheque && string.IsNullOrWhiteSpace(input.Reference))
                return VoucherDomainErrors.ChequeNumberRequired;
        }
        else if (input.PaymentMethod is not null && type != JournalType.CashReceipt && type != JournalType.CashPayment)
            return VoucherDomainErrors.MethodNotAllowed;

        var withholds = type is JournalType.CustomerReceipt or JournalType.SupplierPayment;
        if (input.WithholdingAmount < 0m || (!withholds && input.WithholdingAmount != 0m))
            return VoucherDomainErrors.WithholdingInvalid;
        if (input.WithholdingAmount > 0m && input.WithholdingAccountId is null)
            throw new InvalidOperationException("Tax withheld needs the withholding account the application resolves.");
        if (input.TaxAmount < 0m || (type != JournalType.ExpenseBill && input.TaxAmount != 0m))
            return VoucherDomainErrors.TaxInvalid;
        if (input.TaxAmount > 0m && input.TaxAccountId is null)
            throw new InvalidOperationException("VAT needs the input VAT account the application resolves.");

        if (withLines)
        {
            if (input.Lines.Count == 0)
                return VoucherDomainErrors.LinesRequired;
            if (input.Lines.Count > MaxLines)
                return VoucherDomainErrors.TooManyLines;
            for (var i = 0; i < input.Lines.Count; i++)
                if (input.Lines[i].Amount <= 0m)
                    return VoucherDomainErrors.LineAmountInvalid(i + 1);
        }
        else
        {
            if (input.Lines.Count > 0)
                return VoucherDomainErrors.LinesNotAllowed;
            if (input.Amount < 0m || (input.Amount == 0m && !(withholds && input.WithholdingAmount > 0m)))
                return VoucherDomainErrors.AmountInvalid;
        }

        if (type == JournalType.ExpenseBill)
        {
            if (string.IsNullOrWhiteSpace(input.Reference))
                return VoucherDomainErrors.BillNumberRequired;
            if (input.DueDate is not { } due || due < input.PostingDate)
                return VoucherDomainErrors.DueBeforeBill;
        }

        if (input.Allocations.Count > 0)
        {
            if (!settlesParty)
                return VoucherDomainErrors.AllocationsNotAllowed;
            if (input.Allocations.Any(a => a.Amount <= 0m) || input.Allocations.Select(a => a.OpenItemId).Distinct().Count() != input.Allocations.Count)
                return VoucherDomainErrors.AllocationInvalid;
            if (input.Allocations.Sum(a => a.Amount) > Total(input))
                return VoucherDomainErrors.AllocationsExceedAmount;
        }
        return Result.Success();
    }
}
