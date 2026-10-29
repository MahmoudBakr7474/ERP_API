using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers.GetCustomerExposure;

// Informational only (owner decision): Sales can't know what the customer still owes before Accounting tracks payments,
// so no order is refused because of these amounts. Every committed order of the customer counts, whatever its warehouse.
public sealed class GetCustomerExposureHandler(ICustomerReadStore customers, SalesReferences references)
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
        return new CustomerExposureResponse(
            customer.Value.Id, company.Value.BaseCurrencyCode, IsPartial: true, customer.Value.CreditLimit, customer.Value.IsOnCreditHold,
            customer.Value.PaymentTermDays, position.OpenOrderCount, position.UninvoicedOrderAmount, position.DeliveredNotInvoicedAmount,
            position.InvoicedAmount, position.PendingCreditAmount);
    }
}
