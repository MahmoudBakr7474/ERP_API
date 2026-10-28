using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Purchasing.Application.Contracts.Suppliers;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;

public interface ISupplierReadStore
{
    Task<SupplierDetailsResponse?> GetAsync(Guid supplierId, CancellationToken ct);

    Task<PagedResult<SupplierSummaryResponse>> ListAsync(SupplierFilter filter, int page, int pageSize, CancellationToken ct);

    /// <summary>Suppliers whose Arabic or English name has one of these keys (SupplierNames.Key), for duplicate warnings.</summary>
    Task<IReadOnlyList<SimilarSupplierResponse>> FindSimilarAsync(
        IReadOnlyCollection<string> nameKeys, Guid? exceptSupplierId, CancellationToken ct);
}
