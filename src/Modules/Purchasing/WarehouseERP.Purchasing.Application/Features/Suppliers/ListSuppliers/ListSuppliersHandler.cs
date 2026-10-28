using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Contracts.Suppliers;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.ListSuppliers;

public sealed class ListSuppliersHandler(ISupplierReadStore suppliers, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListSuppliersQuery, PagedResult<SupplierSummaryResponse>>
{
    public async Task<Result<PagedResult<SupplierSummaryResponse>>> HandleAsync(ListSuppliersQuery query, CancellationToken ct) =>
        await suppliers.ListAsync(
            new SupplierFilter(Text.Optional(query.Search), query.IsActive, Text.Optional(query.CountryCode)?.ToUpperInvariant()),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
