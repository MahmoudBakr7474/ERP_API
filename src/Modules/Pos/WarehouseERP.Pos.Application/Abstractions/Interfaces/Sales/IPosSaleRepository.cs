using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Sales;

/// <summary>Write side of POS sales.</summary>
public interface IPosSaleRepository
{
    /// <summary>The sale's warehouse, for the scope check, or null when there is no such sale.</summary>
    Task<Guid?> FindWarehouseAsync(Guid saleId, CancellationToken ct);

    /// <summary>The sale with its lines and payments, tracked and locked (UPDLOCK) until the transaction ends, or null.</summary>
    Task<PosSale?> GetForUpdateAsync(Guid saleId, CancellationToken ct);

    void Add(PosSale sale);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
