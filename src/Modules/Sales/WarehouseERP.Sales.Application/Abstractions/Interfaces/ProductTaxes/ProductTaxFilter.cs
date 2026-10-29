using WarehouseERP.Sales.Contracts;

namespace WarehouseERP.Sales.Application.Abstractions.Interfaces.ProductTaxes;

public sealed record ProductTaxFilter(Guid? ProductId, SalesTaxTreatment? TaxTreatment);
