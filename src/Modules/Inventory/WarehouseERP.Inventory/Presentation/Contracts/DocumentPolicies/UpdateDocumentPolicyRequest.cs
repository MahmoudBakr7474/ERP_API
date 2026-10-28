namespace WarehouseERP.Inventory.Presentation.Contracts.DocumentPolicies;

public sealed record UpdateDocumentPolicyRequest(bool RequiresApproval, string RowVersion);
