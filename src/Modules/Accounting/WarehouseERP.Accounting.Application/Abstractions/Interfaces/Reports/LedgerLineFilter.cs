using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;

/// <param name="AccountIds">Only lines on these accounts; null for every account.</param>
public sealed record LedgerLineFilter(
    DateOnly FromDate, DateOnly ToDate, IReadOnlyCollection<Guid>? AccountIds, Guid? BranchId, JournalType? JournalType);
