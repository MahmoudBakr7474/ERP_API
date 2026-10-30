using Microsoft.Extensions.Options;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.SourcePostings;
using WarehouseERP.Accounting.Application.Contracts.SourcePostings;
using WarehouseERP.Accounting.Application.Shared;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Features.SourcePostings.ListSourcePostings;

// The exception queue and the journaling history: failures (with their reason and next try), pending documents, posted
// ones with their journal, newest first.
public sealed class ListSourcePostingsHandler(ISourcePostingReadStore readStore, ICurrentUser currentUser, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSourcePostingsQuery, PagedResult<SourcePostingResponse>>
{
    public async Task<Result<PagedResult<SourcePostingResponse>>> HandleAsync(ListSourcePostingsQuery query, CancellationToken ct)
    {
        var access = AccountingAccess.CompanyWide(currentUser);
        if (access.IsFailure)
            return access.Error;
        return await readStore.ListAsync(
            new SourcePostingFilter(query.Status, query.SourceType, query.FromDate, query.ToDate, query.VarianceOnly),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
    }
}
