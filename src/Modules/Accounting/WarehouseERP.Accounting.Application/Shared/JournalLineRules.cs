using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Shared;

// What a line may name, checked when a draft is saved and again when it posts (an account, branch or party may have
// changed in between): an existing, active posting account and an existing, active branch. A line on a customer or
// supplier control account names an existing party of that kind, and only such a line names a party, so every control
// balance is someone's. Manual journals use only accounts open to manual postings; opening balances may use system
// accounts (inventory, GRNI); a reversal mirrors what already posted; a voucher's lines were built from its own rules.
public sealed class JournalLineRules(IAccountRepository accounts, AccountingReferences references, PartyDirectory parties)
{
    public static IReadOnlyList<JournalLineInput> ToInputs(IReadOnlyList<JournalLineRequest> lines) =>
        lines.Select(l => new JournalLineInput(l.AccountId, l.BranchId, l.Debit, l.Credit, l.Description, l.PartyType, l.PartyId, l.DueDate))
            .ToList();

    public async Task<Result> CheckAsync(IReadOnlyList<JournalLineInput> lines, JournalType type, CancellationToken ct)
    {
        var byId = (await accounts.FindManyAsync(lines.Select(l => l.AccountId).Distinct().ToList(), ct)).ToDictionary(a => a.Id);
        var branches = await references.BranchesAsync(lines.Select(l => l.BranchId).Distinct().ToList(), ct);
        var named = await parties.FindAsync(
            lines.Where(l => l.PartyType is not null && l.PartyId is not null).Select(l => (l.PartyType!.Value, l.PartyId!.Value)), ct);
        for (var i = 0; i < lines.Count; i++)
        {
            var number = i + 1;
            var line = lines[i];
            var account = byId.GetValueOrDefault(line.AccountId);
            if (account is null)
                return JournalErrors.AccountNotFound(number);
            if (!account.IsPosting)
                return JournalErrors.AccountNotPosting(number);
            if (!account.IsActive)
                return JournalErrors.AccountNotActive(number);
            if (account.PartyType is { } required)
            {
                if (line.PartyId is not { } partyId)
                    return JournalErrors.PartyRequired(number);
                if (line.PartyType != required)
                    return JournalErrors.PartyNotAllowed(number);
                if (!named.ContainsKey((required, partyId)))
                    return JournalErrors.PartyNotFound(number);
            }
            else if (line.PartyId is not null)
                return JournalErrors.PartyNotAllowed(number);
            if (type == JournalType.Manual && !account.AllowsManualPosting)
                return JournalErrors.AccountNotManual(number);
            var branch = branches.GetValueOrDefault(line.BranchId);
            if (branch is null)
                return JournalErrors.BranchNotFound(number);
            if (!branch.IsActive)
                return JournalErrors.BranchNotActive(number);
        }
        return Result.Success();
    }
}
