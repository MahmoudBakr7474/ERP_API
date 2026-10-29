using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers.SetCustomerCredit;

// Credit terms are their own permission (sales.credit.manage): who changed them and when stays on the customer, and its
// temporal history keeps every earlier value. Documents already made keep the payment terms they copied.
public sealed partial class SetCustomerCreditHandler(
    ICustomerRepository customers, ICurrentUser currentUser, TimeProvider clock, ILogger<SetCustomerCreditHandler> logger)
    : ICommandHandler<SetCustomerCreditCommand, string>
{
    public async Task<Result<string>> HandleAsync(SetCustomerCreditCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var customer = await customers.GetForUpdateAsync(command.CustomerId, expected, ct);
        if (customer is null)
            return CustomerErrors.NotFound;
        var set = customer.SetCredit(command.PaymentTermDays, command.CreditLimit, command.IsOnCreditHold, command.CreditHoldReason,
            currentUser.UserId, clock.GetUtcNow().UtcDateTime);
        if (set.IsFailure)
            return set.Error;
        var saved = await customers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        CreditChanged(logger, customer.Id, command.IsOnCreditHold);
        return Convert.ToBase64String(customer.RowVersion);
    }

    [LoggerMessage(EventId = 7004, Level = LogLevel.Information, Message = "Customer {CustomerId} credit terms changed; on hold: {OnHold}")]
    private static partial void CreditChanged(ILogger logger, Guid customerId, bool onHold);
}
