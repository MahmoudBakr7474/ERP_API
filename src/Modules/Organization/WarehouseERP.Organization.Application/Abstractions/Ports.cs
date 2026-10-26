using WarehouseERP.BuildingBlocks.Application.Pagination;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Abstractions;

// Ports this module's use cases need, implemented in WarehouseERP.Organization over the shared
// ErpDbContext. Purpose-specific on purpose: no generic repository.

/// <summary>Write side of warehouses.</summary>
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

/// <summary>Read side of warehouses: projections, no tracking.</summary>
public interface IWarehouseReadStore
{
    Task<WarehouseDetails?> GetAsync(Guid warehouseId, CancellationToken ct);

    Task<PagedResult<WarehouseSummary>> ListAsync(WarehouseListFilter filter, int page, int pageSize, CancellationToken ct);
}

/// <param name="OnlyWarehouseIds">Restricts the list to these warehouses; null means no restriction.</param>
public sealed record WarehouseListFilter(Guid? BranchId, IReadOnlySet<Guid>? OnlyWarehouseIds);

public sealed record WarehouseLocationItem(Guid Id, string Code, string NameAr, string? NameEn, bool IsDefault, bool IsActive);

public sealed record WarehouseDetails(
    Guid Id, Guid BranchId, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling,
    bool IsActive, IReadOnlyList<WarehouseLocationItem> Locations);

public sealed record WarehouseSummary(
    Guid Id, Guid BranchId, string Code, string NameAr, string? NameEn, WarehouseType Type, bool AllowsSelling, bool IsActive);
