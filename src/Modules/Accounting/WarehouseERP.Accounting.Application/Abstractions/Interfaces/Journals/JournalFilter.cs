using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Journals;

/// <param name="Search">Part of the number, draft reference, description or reference.</param>
public sealed record JournalFilter(JournalStatus? Status, JournalType? Type, DateOnly? FromDate, DateOnly? ToDate, string? Search);
