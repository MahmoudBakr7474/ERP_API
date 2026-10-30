namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;

/// <summary>A card charged at a till with no sale recorded, still waiting for its reversal or settlement.</summary>
public sealed record OpenCardIncidentResponse(
    Guid Id, Guid BranchId, Guid TerminalId, Guid? ShiftId, decimal CardAmount, string References, DateTime OccurredAtUtc);
