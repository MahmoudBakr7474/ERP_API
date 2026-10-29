using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Features.Customers.SearchPosCustomers;

// Active registered customers from Sales, for a cashier to name on a sale (pos.sell, no customer permission needed).
public sealed class SearchPosCustomersHandler(ICustomerDirectory customers)
    : IQueryHandler<SearchPosCustomersQuery, IReadOnlyList<PosCustomerResponse>>
{
    private const int DefaultTake = 10;

    public async Task<Result<IReadOnlyList<PosCustomerResponse>>> HandleAsync(SearchPosCustomersQuery query, CancellationToken ct) =>
        Result.Success<IReadOnlyList<PosCustomerResponse>>((await customers.SearchActiveAsync(query.Search, query.Take ?? DefaultTake, ct))
            .Select(c => new PosCustomerResponse(c.Id, c.Code, c.NameAr, c.NameEn, c.CustomerType, c.TaxRegistrationNumber, c.Mobile))
            .ToList());
}
