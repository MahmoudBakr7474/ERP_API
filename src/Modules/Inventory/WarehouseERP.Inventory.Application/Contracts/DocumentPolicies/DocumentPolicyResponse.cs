using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Contracts.DocumentPolicies;

/// <param name="RowVersion">Send it back to change the policy.</param>
public sealed record DocumentPolicyResponse(StockDocumentType DocumentType, bool RequiresApproval, DateTime? UpdatedAtUtc, string RowVersion);
