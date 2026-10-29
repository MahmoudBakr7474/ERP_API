using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers.SetCustomerActive;

// An inactive customer takes no new quotations or orders, and no confirmation of orders still in draft or submitted;
// confirmed orders are delivered, invoiced and returned as usual.
public sealed partial class SetCustomerActiveHandler(ICustomerRepository customers, ILogger<SetCustomerActiveHandler> logger)
    : ICommandHandler<SetCustomerActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetCustomerActiveCommand command, CancellationToken ct)
    {
        var customer = await customers.GetForUpdateAsync(command.CustomerId, expectedRowVersion: null, ct);
        if (customer is null)
            return CustomerErrors.NotFound;
        if (command.IsActive) customer.Activate();
        else customer.Deactivate();
        var saved = await customers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        ActiveChanged(logger, customer.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 7003, Level = LogLevel.Information, Message = "Customer {CustomerId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid customerId, bool isActive);
}
