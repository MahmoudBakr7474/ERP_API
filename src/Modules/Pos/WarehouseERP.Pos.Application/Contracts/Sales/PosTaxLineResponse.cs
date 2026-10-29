using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Sales;

/// <summary>The receipt's VAT summary: net and VAT of all lines with one treatment and rate.</summary>
public sealed record PosTaxLineResponse(PosTaxTreatment TaxTreatment, decimal TaxRate, decimal NetAmount, decimal TaxAmount);
