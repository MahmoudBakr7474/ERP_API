using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Contracts.Suppliers;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.GetSupplier;

public sealed class GetSupplierHandler(ISupplierReadStore suppliers) : IQueryHandler<GetSupplierQuery, SupplierDetailsResponse>
{
    public async Task<Result<SupplierDetailsResponse>> HandleAsync(GetSupplierQuery query, CancellationToken ct) =>
        await suppliers.GetAsync(query.SupplierId, ct) is { } supplier ? supplier : SupplierErrors.NotFound;
}
