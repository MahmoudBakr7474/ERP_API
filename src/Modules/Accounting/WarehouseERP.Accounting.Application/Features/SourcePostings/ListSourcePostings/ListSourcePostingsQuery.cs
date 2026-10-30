using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.ListSourcePostings;

/// <param name="FromDate">Accounting dates from this one (inclusive).</param>
/// <param name="VarianceOnly">Only documents whose journal kept a price or cost difference for review.</param>
public sealed record ListSourcePostingsQuery(
    int Page, int? PageSize, SourcePostingStatus? Status, SourceDocumentType? SourceType, DateOnly? FromDate, DateOnly? ToDate,
    bool VarianceOnly) : IPagedQuery;
