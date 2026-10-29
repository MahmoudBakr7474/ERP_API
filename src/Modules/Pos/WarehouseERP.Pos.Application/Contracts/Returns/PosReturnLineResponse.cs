using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Returns;

public sealed record PosReturnLineResponse(
    Guid Id, int LineNumber, Guid SaleLineId, Guid ProductId, string Sku, string ProductName, Guid ProductUnitId, string UnitCode,
    decimal Quantity, PosReturnCondition Condition, decimal UnitPrice, decimal DiscountPercent, PosTaxTreatment TaxTreatment,
    decimal TaxRate, decimal NetAmount, decimal TaxAmount, decimal TotalAmount);
