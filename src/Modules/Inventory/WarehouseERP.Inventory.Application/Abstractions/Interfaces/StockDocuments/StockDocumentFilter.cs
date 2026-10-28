using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;

/// <param name="ScopeWarehouseIds">The user's warehouses; null for company-wide users.</param>
/// <param name="Search">Matches the number, the draft reference or the external reference.</param>
public sealed record StockDocumentFilter(
    IReadOnlyCollection<Guid>? ScopeWarehouseIds, Guid? WarehouseId, StockDocumentType? DocumentType,
    StockDocumentStatus? Status, string? Search);
