using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals;

// What a line may name, checked when a draft is saved and again when it posts (an account or branch may have changed in
// between): an existing, active posting account and an existing, active branch. Manual journals use only accounts open to
// manual postings; opening balances may use system accounts (inventory, GRNI) too; a reversal mirrors what already posted.
// Customer and supplier control accounts take their lines with a party, in receivables and payables.
public sealed class JournalLineRules(IAccountRepository accounts, AccountingReferences references)
{
    public static IReadOnlyList<JournalLineInput> ToInputs(IReadOnlyList<JournalLineRequest> lines) =>
        lines.Select(l => new JournalLineInput(l.AccountId, l.BranchId, l.Debit, l.Credit, l.Description)).ToList();

    public async Task<Result> CheckAsync(IReadOnlyList<JournalLineInput> lines, JournalType type, CancellationToken ct)
    {
        var byId = (await accounts.FindManyAsync(lines.Select(l => l.AccountId).Distinct().ToList(), ct)).ToDictionary(a => a.Id);
        var branches = await references.BranchesAsync(lines.Select(l => l.BranchId).Distinct().ToList(), ct);
        for (var i = 0; i < lines.Count; i++)
        {
            var number = i + 1;
            var account = byId.GetValueOrDefault(lines[i].AccountId);
            if (account is null)
                return JournalErrors.AccountNotFound(number);
            if (!account.IsPosting)
                return JournalErrors.AccountNotPosting(number);
            if (!account.IsActive)
                return JournalErrors.AccountNotActive(number);
            if (account.PartyType is not null)
                return JournalErrors.PartyLinesNotSupported(number);
            if (type == JournalType.Manual && !account.AllowsManualPosting)
                return JournalErrors.AccountNotManual(number);
            var branch = branches.GetValueOrDefault(lines[i].BranchId);
            if (branch is null)
                return JournalErrors.BranchNotFound(number);
            if (!branch.IsActive)
                return JournalErrors.BranchNotActive(number);
        }
        return Result.Success();
    }
}
