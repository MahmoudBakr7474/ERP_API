using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Contracts.Returns;

/// <param name="CostAmount">What it came back at (its delivery line's cost), once approved.</param>
/// <param name="UninvoicedQuantity">The part that had not been invoiced: it simply won't be.</param>
/// <param name="InvoicedQuantity">The part already invoiced: the company owes the credit below.</param>
public sealed record SalesReturnLineResponse(
    Guid Id, int LineNumber, Guid DeliveryLineId, Guid OrderLineId, Guid ProductId, string Sku, string ProductName,
    Guid ProductUnitId, string UnitCode, decimal Quantity, ReturnCondition Condition, Guid LocationId, string LocationCode,
    Guid? StockLineId, decimal? CostAmount, decimal UninvoicedQuantity, decimal InvoicedQuantity, decimal CreditNetAmount,
    decimal CreditTaxAmount);
