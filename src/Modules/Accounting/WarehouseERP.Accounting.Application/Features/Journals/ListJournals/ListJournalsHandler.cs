using Microsoft.Extensions.Options;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.Journals.ListJournals;

public sealed class ListJournalsHandler(IJournalReadStore journals, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListJournalsQuery, PagedResult<JournalSummaryResponse>>
{
    public async Task<Result<PagedResult<JournalSummaryResponse>>> HandleAsync(ListJournalsQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await journals.ListAsync(
            new JournalFilter(query.Status, query.Type, query.FromDate, query.ToDate, Text.Optional(query.Search)),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
