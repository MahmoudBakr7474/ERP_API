using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Orders;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders.UpdateSalesOrder;

// Only a draft, only from the version the editor read; its lines are priced again as sent. Returns the new row version.
public sealed partial class UpdateSalesOrderHandler(
    ISalesOrderRepository orders, ICustomerReadStore customers, SalesReferences references, LinePricing pricing,
    ICurrentUser currentUser, ILogger<UpdateSalesOrderHandler> logger)
    : ICommandHandler<UpdateSalesOrderCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateSalesOrderCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var access = await OrderAccess.AuthorizeAsync(orders, currentUser, command.OrderId, ct);
        if (access.IsFailure)
            return access.Error;
        var order = await orders.GetForUpdateAsync(command.OrderId, expected, ct);
        if (order is null)
            return OrderErrors.NotFound;
        var customer = await references.ActiveCustomerAsync(command.CustomerId, ct);
        if (customer.IsFailure)
            return customer.Error;
        var address = await ShippingAddresses.ResolveAsync(customers, command.CustomerId, command.ShippingAddressId, ct);
        if (address.IsFailure)
            return address.Error;
        var lines = await pricing.PriceAsync(command.Lines, ct);
        if (lines.IsFailure)
            return lines.Error;
        var replaced = order.ReplaceDraft(new OrderTerms(command.CustomerId, command.OrderDate, command.RequestedDeliveryDate,
            command.CustomerReference, command.PaymentTermDays, address.Value, command.Notes), lines.Value);
        if (replaced.IsFailure)
            return replaced.Error;
        var saved = await orders.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        OrderUpdated(logger, order.Id);
        return Convert.ToBase64String(order.RowVersion);
    }

    [LoggerMessage(EventId = 7031, Level = LogLevel.Information, Message = "Sales order {OrderId} updated")]
    private static partial void OrderUpdated(ILogger logger, Guid orderId);
}
