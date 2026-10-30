using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Mappings;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Settings;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Vouchers;

// Turns what a voucher says into the domain's VoucherInput, resolving every account it needs from data, never from code:
// the cashbox or bank (active) and its ledger account, the party (from Sales or Purchasing) and the control account mapped
// to receivables or payables, the withholding account mapped for the voucher's direction, input VAT for a bill. Counter
// lines may use any active posting account open to manual postings except control accounts (receipts, payments and refunds
// do that) and cashboxes or banks (transfers do that). The open items it plans to settle must be the party's, on its control
// account, on the other side, with enough left open; posting checks them again under the party's lock.
public sealed class VoucherBuilder(
    ICashAccountRepository cashAccounts, PartyDirectory parties, IAccountMappingRepository mappings, IAccountRepository accounts,
    IOpenItemRepository openItems, IAccountingSettingsRepository settings, AccountingReferences references)
{
    public async Task<Result<VoucherInput>> BuildAsync(VoucherFields fields, CancellationToken ct)
    {
        if (!JournalEntry.IsVoucherType(fields.Type))
            return VoucherErrors.NotVoucher;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var policies = await settings.GetAsync(ct);
        if (fields.PostingDate < policies.CutoverDate)
            return JournalErrors.BeforeCutover(policies.CutoverDate);

        var cash = await CashAsync(fields.CashAccountId, ct);
        if (cash.IsFailure)
            return cash.Error;
        var counterCash = await CashAsync(fields.CounterCashAccountId, ct);
        if (counterCash.IsFailure)
            return counterCash.Error;

        VoucherParty? party = null;
        PartyInfo? partyInfo = null;
        if (fields.PartyId is { } partyId && fields.PartyType is { } partyType)
        {
            partyInfo = await parties.FindAsync(partyType, partyId, ct);
            if (partyInfo is null)
                return VoucherErrors.PartyNotFound;
            var control = await MappedAsync(partyType == PartyType.Customer ? PostingRole.AccountsReceivable : PostingRole.AccountsPayable, ct);
            if (control.IsFailure)
                return control.Error;
            party = new VoucherParty(partyType, partyId, control.Value);
        }

        Guid? withholdingAccount = null;
        if (fields.WithholdingAmount > 0m && fields.Type is JournalType.CustomerReceipt or JournalType.SupplierPayment)
        {
            var mapped = await MappedAsync(fields.Type == JournalType.CustomerReceipt
                ? PostingRole.WithholdingTaxReceivable : PostingRole.WithholdingTaxPayable, ct);
            if (mapped.IsFailure)
                return mapped.Error;
            withholdingAccount = mapped.Value;
        }
        Guid? taxAccount = null;
        if (fields.TaxAmount > 0m && fields.Type == JournalType.ExpenseBill)
        {
            var mapped = await MappedAsync(PostingRole.InputVat, ct);
            if (mapped.IsFailure)
                return mapped.Error;
            taxAccount = mapped.Value;
        }

        var counter = await CounterLinesAsync(fields, ct);
        if (counter.IsFailure)
            return counter.Error;
        var branchId = fields.BranchId ?? cash.Value?.BranchId ?? (fields.Lines.Count > 0 ? fields.Lines[0].BranchId : null);
        if (branchId is not { } branch)
            return VoucherErrors.BranchNotFound;
        var branchInfo = (await references.BranchesAsync([branch], ct)).GetValueOrDefault(branch);
        if (branchInfo is null)
            return VoucherErrors.BranchNotFound;
        if (!branchInfo.IsActive)
            return VoucherErrors.BranchNotActive;

        var dueDate = fields.Type == JournalType.ExpenseBill
            ? fields.DueDate ?? fields.PostingDate.AddDays(partyInfo?.PaymentTermDays ?? 0)
            : (DateOnly?)null;
        var allocations = fields.Allocations.Select(a => new VoucherAllocationInput(a.OpenItemId, a.Amount)).ToList();
        if (party is not null && allocations.Count > 0)
        {
            var planned = await CheckAllocationsAsync(fields.Type, party, allocations, ct);
            if (planned.IsFailure)
                return planned.Error;
        }

        return new VoucherInput(fields.Type, fields.PostingDate, fields.Description, fields.Reference, company.Value.BaseCurrencyCode,
            policies.VouchersRequireApproval, branch, cash.Value, counterCash.Value, party, fields.PaymentMethod, fields.Amount,
            fields.WithholdingAmount, withholdingAccount, fields.TaxAmount, taxAccount, dueDate, counter.Value, allocations);
    }

    private async Task<Result<VoucherCash?>> CashAsync(Guid? cashAccountId, CancellationToken ct)
    {
        if (cashAccountId is not { } id)
            return Result.Success<VoucherCash?>(null);
        var account = await cashAccounts.FindAsync(id, ct);
        if (account is null)
            return VoucherErrors.CashAccountNotFound;
        if (!account.IsActive)
            return VoucherErrors.CashAccountNotActive;
        return new VoucherCash(account.Id, account.Kind, account.AccountId, account.BranchId);
    }

    private async Task<Result<Guid>> MappedAsync(PostingRole role, CancellationToken ct)
    {
        var mapping = await mappings.FindAsync(role, ct);
        if (mapping is null)
            return MappingErrors.RoleNotMapped(role);
        var account = await accounts.FindAsync(mapping.AccountId, ct)
            ?? throw new InvalidOperationException($"Role {role} is mapped to account {mapping.AccountId}, which does not exist.");
        var fits = PostingRoleRules.Check(role, account);
        return fits.IsFailure ? fits.Error : account.Id;
    }

    private async Task<Result<IReadOnlyList<VoucherLineInput>>> CounterLinesAsync(VoucherFields fields, CancellationToken ct)
    {
        var ids = fields.Lines.Select(l => l.AccountId).Distinct().ToList();
        var byId = (await accounts.FindManyAsync(ids, ct)).ToDictionary(a => a.Id);
        var treasury = (await cashAccounts.FindByLedgerAccountsAsync(ids, ct)).Select(c => c.AccountId).ToHashSet();
        for (var i = 0; i < fields.Lines.Count; i++)
        {
            var account = byId.GetValueOrDefault(fields.Lines[i].AccountId);
            if (account is not { IsPosting: true, IsActive: true, AllowsManualPosting: true, PartyType: null } || treasury.Contains(account.Id))
                return VoucherErrors.CounterAccountInvalid(i + 1);
        }
        return Result.Success<IReadOnlyList<VoucherLineInput>>(
            fields.Lines.Select(l => new VoucherLineInput(l.AccountId, l.BranchId, l.Amount, l.Description)).ToList());
    }

    // The voucher's own item is a credit for a customer receipt or a supplier refund, a debit for a supplier payment or a
    // customer refund; what it settles sits on the other side.
    private async Task<Result> CheckAllocationsAsync(JournalType type, VoucherParty party, List<VoucherAllocationInput> planned, CancellationToken ct)
    {
        var ownIsDebit = type is JournalType.SupplierPayment or JournalType.CustomerRefund;
        var items = (await openItems.FindAsync(planned.Select(p => p.OpenItemId).ToList(), ct)).ToDictionary(i => i.Id);
        for (var i = 0; i < planned.Count; i++)
        {
            var item = items.GetValueOrDefault(planned[i].OpenItemId);
            if (item is null)
                return VoucherErrors.AllocationItemNotFound(i + 1);
            if (item.PartyType != party.Type || item.PartyId != party.PartyId || item.AccountId != party.ControlAccountId || item.IsDebit == ownIsDebit)
                return VoucherErrors.AllocationItemMismatch(i + 1);
            if (planned[i].Amount > item.Unsettled)
                return VoucherErrors.AllocationExceedsOpen(i + 1);
        }
        return Result.Success();
    }
}
