using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.Journals.ListJournals;

/// <param name="Search">Part of the number, draft reference, description or reference.</param>
public sealed record ListJournalsQuery(
    int Page, int? PageSize, JournalStatus? Status, JournalType? Type, DateOnly? FromDate, DateOnly? ToDate, string? Search) : IPagedQuery;
