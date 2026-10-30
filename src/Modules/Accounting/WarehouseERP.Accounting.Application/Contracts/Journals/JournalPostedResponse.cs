using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Journals;

/// <summary>What posting (directly or by approval) answers; a retry with the same Idempotency-Key gets it again.</summary>
public sealed record JournalPostedResponse(
    Guid Id, string Number, JournalType JournalType, DateOnly PostingDate, decimal TotalDebit, DateTime PostedAtUtc);
