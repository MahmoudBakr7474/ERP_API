using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Journals;

public sealed record JournalEventResponse(JournalAction Action, Guid? UserId, string? UserName, DateTime AtUtc, string? Note);
