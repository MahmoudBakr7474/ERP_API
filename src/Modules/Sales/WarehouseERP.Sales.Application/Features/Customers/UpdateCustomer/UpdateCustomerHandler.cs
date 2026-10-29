using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Application.Shared;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers.UpdateCustomer;

// Only from the version the editor read (409 otherwise). The code, the type and the credit terms stay; documents already
// made keep their own terms and delivery address. A country or city the customer already has may stay after it was
// deactivated; a new one must be active. Returns the new row version.
public sealed partial class UpdateCustomerHandler(
    ICustomerRepository customers, IGeographyDirectory geography, ILogger<UpdateCustomerHandler> logger)
    : ICommandHandler<UpdateCustomerCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateCustomerCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var customer = await customers.GetForUpdateAsync(command.CustomerId, expected, ct);
        if (customer is null)
            return CustomerErrors.NotFound;
        var located = await CustomerGeography.CheckAsync(geography, customer, command.CountryCode, command.Addresses, ct);
        if (located.IsFailure)
            return located.Error;
        var updated = customer.Update(new CustomerDetails(
            command.NameAr, command.NameEn, command.CountryCode, command.TaxRegistrationNumber, command.CommercialRegistrationNumber,
            command.CommercialRegistry, command.Phone, command.Mobile, command.Email, command.Notes,
            CustomerRules.Contacts(command.Contacts), CustomerRules.Addresses(command.Addresses)));
        if (updated.IsFailure)
            return updated.Error;
        var unique = await CustomerRegistrations.CheckUniqueAsync(customers, customer, customer.Id, ct);
        if (unique.IsFailure)
            return unique.Error;

        var saved = await customers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;
        CustomerUpdated(logger, customer.Id);
        return Convert.ToBase64String(customer.RowVersion);
    }

    [LoggerMessage(EventId = 7002, Level = LogLevel.Information, Message = "Customer {CustomerId} updated")]
    private static partial void CustomerUpdated(ILogger logger, Guid customerId);
}
