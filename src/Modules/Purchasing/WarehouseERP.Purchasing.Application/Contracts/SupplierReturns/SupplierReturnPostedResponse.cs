namespace WarehouseERP.Purchasing.Application.Contracts.SupplierReturns;

/// <summary>What approving a return did; a retry with the same Idempotency-Key gets the same answer.</summary>
/// <param name="Number">SRN-…</param>
/// <param name="StockDocumentNumber">PRT-…, the PurchaseReturn stock document Inventory posted.</param>
/// <param name="CreditClaimTotalAmount">Pending credit the supplier owes for returned goods already invoiced; 0 when none.</param>
public sealed record SupplierReturnPostedResponse(
    Guid Id, string Number, Guid StockDocumentId, string StockDocumentNumber, decimal CreditClaimTotalAmount, DateTime PostedAtUtc);
