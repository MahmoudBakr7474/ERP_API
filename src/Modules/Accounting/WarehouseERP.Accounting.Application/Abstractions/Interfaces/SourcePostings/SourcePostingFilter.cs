using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;

/// <param name="VarianceOnly">Only documents whose journal kept a price or cost difference for review.</param>
public sealed record SourcePostingFilter(
    SourcePostingStatus? Status, SourceDocumentType? SourceType, DateOnly? FromDate, DateOnly? ToDate, bool VarianceOnly);
