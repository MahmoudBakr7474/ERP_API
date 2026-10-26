namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;

/// <param name="OnlyWarehouseIds">Restricts the list to these warehouses; null means no restriction.</param>
public sealed record WarehouseListFilter(Guid? BranchId, IReadOnlySet<Guid>? OnlyWarehouseIds);
