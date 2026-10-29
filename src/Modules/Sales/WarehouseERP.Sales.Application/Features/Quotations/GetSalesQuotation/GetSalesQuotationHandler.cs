using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Quotations;
using WarehouseERP.Sales.Application.Contracts.Quotations;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Quotations.GetSalesQuotation;

public sealed class GetSalesQuotationHandler(
    ISalesQuotationRepository quotations, ISalesQuotationReadStore readStore, SalesReferences references, ICurrentUser currentUser)
    : IQueryHandler<GetSalesQuotationQuery, SalesQuotationDetailsResponse>
{
    public async Task<Result<SalesQuotationDetailsResponse>> HandleAsync(GetSalesQuotationQuery query, CancellationToken ct)
    {
        var access = await QuotationAccess.AuthorizeAsync(quotations, currentUser, query.QuotationId, ct);
        if (access.IsFailure)
            return access.Error;
        var today = await references.TodayAsync(ct);
        if (today.IsFailure)
            return today.Error;
        return await readStore.GetAsync(query.QuotationId, today.Value, ct) is { } quotation ? quotation : QuotationErrors.NotFound;
    }
}
