using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.CashAccounts;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Shared;

// The locks every posting takes before its period's, inside the caller's transaction: its parties' locks, then its
// cashboxes' and banks' locks, each in a fixed order, so two postings never wait on each other crosswise. A cashbox, or a
// bank without overdraft, the journal would take below zero at the end of its date or of any later day refuses it.
public sealed class PostingGuard(ICashAccountRepository cashAccounts, IAccountRepository accounts, IAccountingLocks locks)
{
    public async Task<Result> LockAndCheckAsync(JournalEntry journal, CancellationToken ct)
    {
        foreach (var (type, id) in journal.Lines.Where(l => l.HasParty).Select(l => (l.PartyType!.Value, l.PartyId!.Value)).Distinct()
                     .OrderBy(p => p.Item1).ThenBy(p => p.Item2))
            if (!await locks.TryLockPartyAsync(type, id, ct))
                return JournalErrors.Busy;
        var cash = (await cashAccounts.FindByLedgerAccountsAsync(journal.Lines.Select(l => l.AccountId).Distinct().ToList(), ct))
            .OrderBy(c => c.Id).ToList();
        foreach (var account in cash)
        {
            if (!await locks.TryLockCashAccountAsync(account.Id, ct))
                return JournalErrors.Busy;
            var change = journal.Lines.Where(l => l.AccountId == account.AccountId).Sum(l => l.Debit - l.Credit);
            if (account.MustStayPositive && change < 0m
                && await accounts.LowestBalanceFromAsync(account.AccountId, journal.PostingDate, ct) + change < 0m)
                return JournalErrors.CashAccountOverdrawn(account.Code);
        }
        return Result.Success();
    }
}
