namespace WarehouseERP.Purchasing.Presentation.Contracts.PurchasingPolicies;

public sealed record UpdatePurchasingPolicyRequest(bool RequiresApproval, string RowVersion);
