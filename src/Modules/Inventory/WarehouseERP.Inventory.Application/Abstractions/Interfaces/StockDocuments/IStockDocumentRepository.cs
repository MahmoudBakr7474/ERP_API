using WarehouseERP.Inventory.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.StockDocuments;

/// <summary>Write side of stock documents with their lines, allocations and history (one aggregate).</summary>
public interface IStockDocumentRepository
{
    /// <summary>What decides who may touch the document, or null when no document has this id.</summary>
    Task<StockDocumentHeader?> FindHeaderAsync(Guid documentId, CancellationToken ct);

    /// <summary>
    /// The document with its lines and allocations, tracked for changes and locked against other writers until the
    /// transaction ends, or null. With a row version, the save fails with a conflict unless the document still has it.
    /// </summary>
    Task<StockDocument?> GetForUpdateAsync(Guid documentId, byte[]? expectedRowVersion, CancellationToken ct);

    /// <summary>True once something other than opening stock (or its reversal) has posted in the warehouse.</summary>
    Task<bool> HasPostedBeyondOpeningAsync(Guid warehouseId, CancellationToken ct);

    /// <summary>True while the document has a reversal that is not cancelled.</summary>
    Task<bool> HasReversalAsync(Guid documentId, CancellationToken ct);

    /// <summary>True once a stock document was made for this Purchasing goods receipt or supplier return.</summary>
    Task<bool> HasSourceAsync(Guid sourceDocumentId, CancellationToken ct);

    void Add(StockDocument document);

    /// <summary>
    /// The use case's save point. A document whose lines or history changed is saved as changed too, so its row version
    /// moves and guards the whole aggregate.
    /// </summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
