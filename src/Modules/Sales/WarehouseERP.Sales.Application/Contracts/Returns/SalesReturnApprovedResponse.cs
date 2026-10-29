namespace WarehouseERP.Sales.Application.Contracts.Returns;

/// <summary>An approved customer return; a retry with the same Idempotency-Key gets the same answer.</summary>
/// <param name="StockDocumentNumber">The SRT-… stock document that took the goods back.</param>
/// <param name="CreditTotalAmount">The pending customer credit it recorded (0 when nothing returned had been invoiced).</param>
public sealed record SalesReturnApprovedResponse(
    Guid ReturnId, string Number, Guid StockDocumentId, string StockDocumentNumber, decimal CostAmount, decimal CreditTotalAmount,
    DateTime ApprovedAtUtc);
