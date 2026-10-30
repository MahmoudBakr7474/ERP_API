using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Contracts;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Allocations.AutoAllocate;

// Oldest first under the party's lock: on each control account, debit items by due date (then date) are settled from
// credit items by date, each allocation as large as both still allow, until one side runs out.
public sealed partial class AutoAllocateHandler(
    ITransactionRunner transactions, IOpenItemRepository openItems, IAccountingLocks locks, ICurrentUser currentUser, TimeProvider clock,
    ILogger<AutoAllocateHandler> logger)
    : ICommandHandler<AutoAllocateCommand, AutoAllocatedResponse>
{
    public async Task<Result<AutoAllocatedResponse>> HandleAsync(AutoAllocateCommand command, CancellationToken ct)
    {
        var access = AllocationAccess.CanAllocate(currentUser, command.PartyType);
        if (access.IsFailure)
            return access.Error;
        return await transactions.RunAsync(token => AllocateOnceAsync(command.PartyType, command.PartyId, token), ct);
    }

    private async Task<Result<AutoAllocatedResponse>> AllocateOnceAsync(PartyType partyType, Guid partyId, CancellationToken ct)
    {
        if (!await locks.TryLockPartyAsync(partyType, partyId, ct))
            return AllocationErrors.Busy;
        var now = clock.GetUtcNow().UtcDateTime;
        var made = 0;
        var total = 0m;
        foreach (var account in (await openItems.ListOpenForUpdateAsync(partyType, partyId, ct)).GroupBy(i => i.AccountId))
        {
            var debits = new Queue<OpenItem>(account.Where(i => i.IsDebit).OrderBy(i => i.DueDate).ThenBy(i => i.DocumentDate).ThenBy(i => i.DocumentNumber));
            var credits = new Queue<OpenItem>(account.Where(i => !i.IsDebit).OrderBy(i => i.DocumentDate).ThenBy(i => i.DocumentNumber));
            while (debits.TryPeek(out var debit) && credits.TryPeek(out var credit))
            {
                var amount = Math.Min(debit.Unsettled, credit.Unsettled);
                var allocation = Allocation.Create(debit, credit, amount, AllocationAccess.SettlesOn(debit, credit), null, currentUser.UserId, now);
                if (allocation.IsFailure)
                    throw new InvalidOperationException($"Matching open items {debit.Id} and {credit.Id} failed: {allocation.Error}");
                openItems.Add(allocation.Value);
                made++;
                total += amount;
                if (debit.IsSettled)
                    debits.Dequeue();
                if (credit.IsSettled)
                    credits.Dequeue();
            }
        }
        if (made > 0)
        {
            var saved = await openItems.SaveChangesAsync(ct);
            if (saved.IsFailure)
                return saved.Error;
        }
        AutoAllocated(logger, partyType, partyId, made, total);
        return new AutoAllocatedResponse(made, total);
    }

    [LoggerMessage(EventId = 9062, Level = LogLevel.Information, Message = "{PartyType} {PartyId}: {Count} allocations made automatically for {Amount}")]
    private static partial void AutoAllocated(ILogger logger, PartyType partyType, Guid partyId, int count, decimal amount);
}
