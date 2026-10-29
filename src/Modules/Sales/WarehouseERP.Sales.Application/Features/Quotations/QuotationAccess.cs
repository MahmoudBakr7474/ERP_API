using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations;

// A quotation belongs to its warehouse's scope. The endpoint already checked the permission of the action.
internal static class QuotationAccess
{
    public static async Task<Result> AuthorizeAsync(ISalesQuotationRepository quotations, ICurrentUser user, Guid quotationId, CancellationToken ct) =>
        DocumentAccess.Check(user, await quotations.FindWarehouseAsync(quotationId, ct), QuotationErrors.NotFound);
}
