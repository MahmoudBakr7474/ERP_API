using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Application.Features.DocumentPolicies.UpdateDocumentPolicy;

public sealed record UpdateDocumentPolicyCommand(StockDocumentType DocumentType, bool RequiresApproval, string RowVersion);
