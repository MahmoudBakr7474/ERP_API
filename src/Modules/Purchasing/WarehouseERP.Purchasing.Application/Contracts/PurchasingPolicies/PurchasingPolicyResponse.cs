using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Contracts.PurchasingPolicies;

/// <param name="RowVersion">Send it back to change the policy.</param>
public sealed record PurchasingPolicyResponse(PurchasingDocumentType DocumentType, bool RequiresApproval, string RowVersion);
