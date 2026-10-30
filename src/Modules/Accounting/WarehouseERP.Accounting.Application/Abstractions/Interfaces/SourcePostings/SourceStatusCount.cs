using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;

public sealed record SourceStatusCount(SourceDocumentType SourceType, SourcePostingStatus Status, int Count);
