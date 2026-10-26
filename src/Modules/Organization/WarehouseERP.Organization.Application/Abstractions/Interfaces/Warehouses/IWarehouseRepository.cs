using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;

/// <summary>Write side of warehouses; implemented over the shared ErpDbContext (no generic repository).</summary>
public interface IWarehouseRepository
{
    Task<bool> IsBranchActiveAsync(Guid branchId, CancellationToken ct);

    Task<bool> CodeExistsAsync(Guid branchId, string code, CancellationToken ct);

    /// <summary>
    /// The use case's save point: the warehouse and its locations in one SaveChanges (one database
    /// transaction). A known unique-key violation comes back as its error, not an exception.
    /// </summary>
    Task<Result> AddAsync(Warehouse warehouse, CancellationToken ct);
}
