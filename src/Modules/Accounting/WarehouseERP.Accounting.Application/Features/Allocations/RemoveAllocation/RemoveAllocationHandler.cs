using Microsoft.Extensions.Logging;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Application.Errors;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Persistence;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Allocations.RemoveAllocation;

// Puts an allocation's amount back on both items (to re-match them, or before reversing one of their journals); the row
// stays, marked removed, by whom and when.
public sealed partial class RemoveAllocationHandler(
    ITransactionRunner transactions, IOpenItemRepository openItems, IAccountingLocks locks, ICurrentUser currentUser, TimeProvider clock,
    ILogger<RemoveAllocationHandler> logger)
    : ICommandHandler<RemoveAllocationCommand, Done>
{
    public Task<Result<Done>> HandleAsync(RemoveAllocationCommand command, CancellationToken ct) =>
        transactions.RunAsync(token => RemoveOnceAsync(command.AllocationId, token), ct);

    private async Task<Result<Done>> RemoveOnceAsync(Guid allocationId, CancellationToken ct)
    {
        var seen = await openItems.FindAllocationAsync(allocationId, ct);
        if (seen is null)
            return AllocationErrors.NotFound;
        var access = AllocationAccess.CanAllocate(currentUser, seen.PartyType);
        if (access.IsFailure)
            return access.Error;
        if (!await locks.TryLockPartyAsync(seen.PartyType, seen.PartyId, ct))
            return AllocationErrors.Busy;
        var allocation = await openItems.GetAllocationForUpdateAsync(allocationId, ct)
            ?? throw new InvalidOperationException($"Allocation {allocationId} disappeared while its party was locked.");
        var items = await openItems.GetForUpdateAsync([allocation.DebitItemId, allocation.CreditItemId], ct);
        var removed = allocation.Remove(items.Single(i => i.Id == allocation.DebitItemId), items.Single(i => i.Id == allocation.CreditItemId),
            currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (removed.IsFailure)
            return removed.Error;
        var saved = await openItems.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        AllocationRemoved(logger, allocation.Id, allocation.Amount);
        return Done.Value;
    }

    [LoggerMessage(EventId = 9061, Level = LogLevel.Information, Message = "Allocation {AllocationId} of {Amount} removed")]
    private static partial void AllocationRemoved(ILogger logger, Guid allocationId, decimal amount);
}
