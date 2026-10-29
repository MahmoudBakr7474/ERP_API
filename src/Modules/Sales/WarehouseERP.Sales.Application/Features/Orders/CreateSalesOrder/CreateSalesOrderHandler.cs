using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders.CreateSalesOrder;

// A draft order of an active customer from one selling warehouse in the user's scope, in the company's base currency, its
// lines priced from the price list (or by a user allowed to set prices) as a snapshot. Nothing is reserved until it is
// confirmed.
public sealed partial class CreateSalesOrderHandler(
    ISalesOrderRepository orders, ICustomerReadStore customers, SalesReferences references, LinePricing pricing,
    ICurrentUser currentUser, TimeProvider clock, ILogger<CreateSalesOrderHandler> logger)
    : ICommandHandler<CreateSalesOrderCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateSalesOrderCommand command, CancellationToken ct)
    {
        if (!currentUser.CanAccessWarehouse(command.WarehouseId))
            return AccessErrors.WarehouseDenied;
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var warehouse = await references.SellingWarehouseAsync(command.WarehouseId, ct);
        if (warehouse.IsFailure)
            return warehouse.Error;
        var customer = await references.ActiveCustomerAsync(command.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        var address = await ShippingAddresses.ResolveAsync(customers, command.CustomerId, command.ShippingAddressId, ct);
        if (address.IsFailure)
            return address.Error;
        var lines = await pricing.PriceAsync(command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;

        var terms = new OrderTerms(command.CustomerId, command.OrderDate ?? references.Today(company.Value), command.RequestedDeliveryDate,
            command.CustomerReference, command.PaymentTermDays ?? customer.Value.PaymentTermDays, address.Value, command.Notes);
        var created = SalesOrder.CreateDraft(command.WarehouseId, warehouse.Value.BranchId, company.Value.BaseCurrencyCode, terms,
            lines.Value, sourceQuotationId: null, currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (created.IsFailure)
            return created.Error;
        orders.Add(created.Value);
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        OrderCreated(logger, created.Value.Id, command.CustomerId);
        return created.Value.Id;
    }

    [LoggerMessage(EventId = 7030, Level = LogLevel.Information, Message = "Sales order {OrderId} drafted for customer {CustomerId}")]
    private static partial void OrderCreated(ILogger logger, Guid orderId, Guid customerId);
}
