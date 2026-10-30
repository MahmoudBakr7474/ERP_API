using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers.GetCustomerExposure;

// Informational only (owner decision): no order is refused because of these amounts. Every committed order of the customer
// counts, whatever its warehouse. Once Accounting is active, its receivable balance holds what was invoiced (less receipts
// and credits); invoices and credits posted since the cutover that are not journaled yet are added from Sales' own
// documents, then what is not invoiced yet (V1 Accounting decision 7: nothing counted twice, nothing left out meanwhile).
public sealed class GetCustomerExposureHandler(ICustomerReadStore customers, SalesReferences references, ICustomerReceivables receivables)
    : IQueryHandler<GetCustomerExposureQuery, CustomerExposureResponse>
{
    public async Task<Result<CustomerExposureResponse>> HandleAsync(GetCustomerExposureQuery query, CancellationToken ct)
    {
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var customer = await references.CustomerAsync(query.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        var position = await customers.GetOrderPositionAsync(query.CustomerId, ct);
        if (position is null)
            return CustomerErrors.NotFound;
        var receivable = await receivables.GetAsync(query.CustomerId, ct);
        decimal? unjournaled = null;
        if (receivable is not null)
        {
            var journaled = receivable.JournaledDocumentIds.ToHashSet();
            unjournaled = (await customers.ListReceivableDocumentsAsync(query.CustomerId, receivable.CutoverStartUtc, ct))
                .Where(d => !journaled.Contains(d.Id)).Sum(d => d.Amount);
        }
        var total = receivable?.Balance + unjournaled + position.UninvoicedOrderAmount;
        return new CustomerExposureResponse(
            customer.Value.Id, company.Value.BaseCurrencyCode, IsPartial: receivable is null, customer.Value.CreditLimit,
            customer.Value.IsOnCreditHold, customer.Value.PaymentTermDays, position.OpenOrderCount, position.UninvoicedOrderAmount,
            position.DeliveredNotInvoicedAmount, position.InvoicedAmount, position.PendingCreditAmount, receivable?.Balance, total,
            customer.Value.CreditLimit - total, unjournaled);
    }
}
