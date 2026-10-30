using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.GetSourceReconciliation;

public sealed record SourceStatusCountResponse(SourceDocumentType SourceType, SourcePostingStatus Status, int Count);
