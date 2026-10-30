using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;

/// <summary>Journal number series per type and fiscal year, advanced inside the posting transaction.</summary>
public interface IDocumentNumbers
{
    /// <summary>The next number of the series; its row stays locked until the transaction ends, so a rollback gives it back.</summary>
    Task<int> NextAsync(JournalType journalType, Guid fiscalYearId, CancellationToken ct);
}
