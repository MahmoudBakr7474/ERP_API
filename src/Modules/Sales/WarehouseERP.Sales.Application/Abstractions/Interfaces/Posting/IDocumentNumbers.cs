using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Posting;

public interface IDocumentNumbers
{
    /// <summary>The series' next number, taken inside the numbering transaction: a rollback leaves no gap.</summary>
    Task<int> NextAsync(SalesDocumentType documentType, Guid branchId, short fiscalYear, CancellationToken ct);
}
