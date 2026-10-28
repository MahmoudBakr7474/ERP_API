using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.Documents;

/// <summary>One step in a purchasing document's history.</summary>
/// <param name="UserId">Who did it; the same user may create, submit and approve a document.</param>
/// <param name="Note">A rejection's or closure's reason, or the number of the receipt, invoice or return it records.</param>
public sealed record DocumentEventResponse(PurchasingDocumentAction Action, Guid? UserId, string? UserName, DateTime AtUtc, string? Note);
