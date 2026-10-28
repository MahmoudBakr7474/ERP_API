using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Posting;

public interface IDocumentNumbers
{
    /// <summary>The series' next number, taken inside the approving or posting transaction: a rollback leaves no gap.</summary>
    Task<int> NextAsync(PurchasingDocumentType documentType, Guid branchId, short fiscalYear, CancellationToken ct);
}
