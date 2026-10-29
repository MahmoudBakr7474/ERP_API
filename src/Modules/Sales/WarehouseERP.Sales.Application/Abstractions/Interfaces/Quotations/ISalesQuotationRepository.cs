using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;

/// <summary>Write side of quotations with their lines and history (one aggregate).</summary>
public interface ISalesQuotationRepository
{
    /// <summary>The quotation's warehouse, which decides who may see it; null when no quotation has this id.</summary>
    Task<Guid?> FindWarehouseAsync(Guid quotationId, CancellationToken ct);

    /// <summary>
    /// The quotation with its lines, tracked and locked against other writers until the transaction ends, or null. With a
    /// row version, the save fails with a conflict unless the quotation still has it.
    /// </summary>
    Task<SalesQuotation?> GetForUpdateAsync(Guid quotationId, byte[]? expectedRowVersion, CancellationToken ct);

    void Add(SalesQuotation quotation);

    /// <summary>The use case's save point; a quotation whose lines or history changed moves its row version too.</summary>
    Task<Result> SaveChangesAsync(CancellationToken ct);
}
