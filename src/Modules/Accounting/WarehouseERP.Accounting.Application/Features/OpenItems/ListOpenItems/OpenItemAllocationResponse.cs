namespace WarehouseERP.Accounting.Application.Features.OpenItems.ListOpenItems;

/// <param name="OtherItemId">The item on the other side of the allocation.</param>
/// <param name="JournalId">The voucher or reversal that made it; null when allocated by hand.</param>
public sealed record OpenItemAllocationResponse(Guid AllocationId, Guid OtherItemId, decimal Amount, DateOnly AllocatedOn, Guid? JournalId);
