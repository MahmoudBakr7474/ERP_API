using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Abstractions.Interfaces.Posting;

public interface IDocumentNumbers
{
    /// <summary>The terminal series' next number, taken inside the numbering transaction: a rollback leaves no gap.</summary>
    Task<int> NextAsync(PosDocumentType documentType, Guid terminalId, short fiscalYear, CancellationToken ct);
}
