using Microsoft.Extensions.Options;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Contracts.Customers;
using WarehouseERP.SharedKernel.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers.ListCustomers;

// Customers are company-wide master data: everyone with the read permission sees all of them, by code.
public sealed class ListCustomersHandler(ICustomerReadStore customers, IOptions<PaginationOptions> pagination)
    : IQueryHandler<ListCustomersQuery, PagedResult<CustomerSummaryResponse>>
{
    public async Task<Result<PagedResult<CustomerSummaryResponse>>> HandleAsync(ListCustomersQuery query, CancellationToken ct) =>
        await customers.ListAsync(
            new CustomerFilter(Text.Optional(query.Search), query.IsActive, query.CustomerType, query.IsOnCreditHold,
                Text.Optional(query.CountryCode)?.ToUpperInvariant()),
            query.Page, query.ResolvePageSize(pagination.Value), ct);
}
