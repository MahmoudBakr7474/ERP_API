using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Contracts.Suppliers;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.FindSimilarSuppliers;

// A warning, never a refusal: two suppliers may share a name. Names match on their normalized keys (SupplierNames).
public sealed class FindSimilarSuppliersHandler(ISupplierReadStore suppliers)
    : IQueryHandler<FindSimilarSuppliersQuery, IReadOnlyList<SimilarSupplierResponse>>
{
    public async Task<Result<IReadOnlyList<SimilarSupplierResponse>>> HandleAsync(FindSimilarSuppliersQuery query, CancellationToken ct)
    {
        var keys = new List<string> { SupplierNames.Key(query.NameAr) };
        if (!string.IsNullOrWhiteSpace(query.NameEn))
            keys.Add(SupplierNames.Key(query.NameEn));
        return Result.Success(await suppliers.FindSimilarAsync(keys.Distinct().ToList(), query.ExceptSupplierId, ct));
    }
}
