namespace WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;

/// <param name="Search">Matches the code or either name.</param>
/// <param name="ThroughWarehouseIds">Only branches holding one of these warehouses; null means no restriction.</param>
public sealed record BranchListFilter(string? Search, bool? IsActive, IReadOnlySet<Guid>? ThroughWarehouseIds);
