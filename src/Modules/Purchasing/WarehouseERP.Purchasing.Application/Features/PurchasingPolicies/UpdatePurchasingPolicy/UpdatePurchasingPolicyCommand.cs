using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Application.Features.PurchasingPolicies.UpdatePurchasingPolicy;

/// <param name="DocumentType">PurchaseRequest or PurchaseOrder.</param>
/// <param name="RowVersion">The version the editor read.</param>
public sealed record UpdatePurchasingPolicyCommand(PurchasingDocumentType DocumentType, bool RequiresApproval, string RowVersion);
