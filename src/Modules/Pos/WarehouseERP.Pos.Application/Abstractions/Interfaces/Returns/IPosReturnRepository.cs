using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Returns;

/// <summary>Write side of POS returns.</summary>
public interface IPosReturnRepository
{
    /// <summary>The return's warehouse, for the scope check, or null when there is no such return.</summary>
    Task<Guid?> FindWarehouseAsync(Guid returnId, CancellationToken ct);

    /// <summary>The return, tracked, or null.</summary>
    Task<PosReturn?> GetForUpdateAsync(Guid returnId, CancellationToken ct);

    void Add(PosReturn posReturn);

    Task<Result> SaveChangesAsync(CancellationToken ct);
}
