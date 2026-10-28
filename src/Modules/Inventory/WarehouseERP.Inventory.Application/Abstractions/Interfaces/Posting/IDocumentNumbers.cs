using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

public interface IDocumentNumbers
{
    /// <summary>The series' next number, taken inside the posting's transaction: a failed posting leaves no gap.</summary>
    Task<int> NextAsync(StockDocumentType documentType, Guid branchId, short fiscalYear, CancellationToken ct);
}
