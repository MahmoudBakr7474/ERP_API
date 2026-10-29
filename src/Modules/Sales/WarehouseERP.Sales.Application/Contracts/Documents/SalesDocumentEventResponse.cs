using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Documents;

/// <summary>One step in a sales document's history.</summary>
/// <param name="UserId">Who did it; the same user may create, submit and approve a document.</param>
/// <param name="Note">A reason, the approval reasons, or the number of the document it records.</param>
public sealed record SalesDocumentEventResponse(SalesDocumentAction Action, Guid? UserId, string? UserName, DateTime AtUtc, string? Note);
