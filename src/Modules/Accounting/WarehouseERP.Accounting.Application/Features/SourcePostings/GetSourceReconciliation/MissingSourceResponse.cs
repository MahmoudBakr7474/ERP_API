using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;

/// <summary>A document posted since the cutover that has no accounting identity yet (the next run finds it).</summary>
public sealed record MissingSourceResponse(SourceDocumentType SourceType, Guid SourceId, string Number, DateTime OccurredAtUtc);
