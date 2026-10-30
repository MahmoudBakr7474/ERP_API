using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.Accounting.Domain;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Allocations.AllocateOpenItems;

// Under the party's lock (so a receipt posting or another allocation can't use the same open amount), both items are read
// again and settled by the amount; the ledger doesn't change.
public sealed partial class AllocateOpenItemsHandler(
    ITransactionRunner transactions, IOpenItemRepository openItems, IAccountingLocks locks, ICurrentUser currentUser, TimeProvider clock,
    ILogger<AllocateOpenItemsHandler> logger)
    : ICommandHandler<AllocateOpenItemsCommand, Guid>
{
    public Task<Result<Guid>> HandleAsync(AllocateOpenItemsCommand command, CancellationToken ct) =>
        transactions.RunAsync(token => AllocateOnceAsync(command, token), ct);

    private async Task<Result<Guid>> AllocateOnceAsync(AllocateOpenItemsCommand command, CancellationToken ct)
    {
        var seen = (await openItems.FindAsync([command.DebitItemId, command.CreditItemId], ct)).SingleOrDefault(i => i.Id == command.DebitItemId);
        if (seen is null)
            return AllocationErrors.ItemNotFound;
        var access = AllocationAccess.CanAllocate(currentUser, seen.PartyType);
        if (access.IsFailure)
            return access.Error;
        if (!await locks.TryLockPartyAsync(seen.PartyType, seen.PartyId, ct))
            return AllocationErrors.Busy;
        var items = await openItems.GetForUpdateAsync([command.DebitItemId, command.CreditItemId], ct);
        var debit = items.SingleOrDefault(i => i.Id == command.DebitItemId);
        var credit = items.SingleOrDefault(i => i.Id == command.CreditItemId);
        if (debit is null || credit is null)
            return AllocationErrors.ItemNotFound;
        var allocation = Allocation.Create(debit, credit, command.Amount, AllocationAccess.SettlesOn(debit, credit), null, currentUser.UserId,
            clock.GetUtcNow().UtcDateTime);
        if (allocation.IsFailure)
            return allocation.Error;
        openItems.Add(allocation.Value);
        var saved = await openItems.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        Allocated(logger, debit.Id, credit.Id, command.Amount);
        return allocation.Value.Id;
    }

    [LoggerMessage(EventId = 9060, Level = LogLevel.Information, Message = "Open item {DebitItemId} settled against {CreditItemId} by {Amount}")]
    private static partial void Allocated(ILogger logger, Guid debitItemId, Guid creditItemId, decimal amount);
}
