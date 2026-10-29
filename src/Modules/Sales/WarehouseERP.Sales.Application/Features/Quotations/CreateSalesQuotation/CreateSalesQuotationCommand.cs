using WarehouseERP.Sales.Application.Contracts.Documents;

namespace WarehouseERP.Sales.Application.Features.Quotations.CreateSalesQuotation;

/// <param name="CustomerId">An active customer.</param>
/// <param name="WarehouseId">An active physical warehouse that allows selling, in the user's scope; fixed once created. Its branch numbers the quotation.</param>
/// <param name="QuotationDate">Today (company time zone) when left out.</param>
/// <param name="ValidUntil">The quotation date plus the settings' validity days when left out.</param>
/// <param name="PaymentTermDays">The customer's terms when left out.</param>
public sealed record CreateSalesQuotationCommand(
    Guid CustomerId, Guid WarehouseId, DateOnly? QuotationDate, DateOnly? ValidUntil, string? CustomerReference, int? PaymentTermDays,
    string? Notes, IReadOnlyList<SalesLineRequest> Lines);
