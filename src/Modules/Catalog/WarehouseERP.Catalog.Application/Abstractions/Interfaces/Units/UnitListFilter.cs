namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Units;

/// <param name="Search">Matches the code or either name.</param>
public sealed record UnitListFilter(string? Search, bool? IsActive);
