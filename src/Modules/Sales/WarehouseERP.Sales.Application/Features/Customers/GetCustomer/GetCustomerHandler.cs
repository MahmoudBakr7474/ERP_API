using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Contracts.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers.GetCustomer;

public sealed class GetCustomerHandler(ICustomerReadStore customers) : IQueryHandler<GetCustomerQuery, CustomerDetailsResponse>
{
    public async Task<Result<CustomerDetailsResponse>> HandleAsync(GetCustomerQuery query, CancellationToken ct) =>
        await customers.GetAsync(query.CustomerId, ct) is { } customer ? customer : CustomerErrors.NotFound;
}
