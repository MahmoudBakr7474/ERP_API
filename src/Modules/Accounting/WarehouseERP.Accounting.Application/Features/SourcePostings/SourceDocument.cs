using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings;

/// <summary>A document a module reports as posted, as an accounting source.</summary>
public sealed record SourceDocument(SourceDocumentType Type, Guid Id, string Number, DateTime OccurredAtUtc);
