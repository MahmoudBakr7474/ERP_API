using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers.CreateCustomer;

// One save point. The pre-checks give clear errors; the database's unique indexes still guard the code and the
// registrations when two requests race. A similar name or the same mobile is only a warning (FindSimilarCustomers), never
// refused here. The country and the addresses' cities must be active entries of Organization's lookups.
public sealed partial class CreateCustomerHandler(
    ICustomerRepository customers, IGeographyDirectory geography, ILogger<CreateCustomerHandler> logger)
    : ICommandHandler<CreateCustomerCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateCustomerCommand command, CancellationToken ct)
    {
        var created = Customer.Create(command.Code, command.CustomerType, new CustomerDetails(
            command.NameAr, command.NameEn, command.CountryCode, command.TaxRegistrationNumber, command.CommercialRegistrationNumber,
            command.CommercialRegistry, command.Phone, command.Mobile, command.Email, command.Notes,
            CustomerRules.Contacts(command.Contacts), CustomerRules.Addresses(command.Addresses)));
        if (created.IsFailure)
            return created.Error;
        var customer = created.Value;
        var located = await CustomerGeography.CheckAsync(geography, current: null, command.CountryCode, command.Addresses, ct);
        if (located.IsFailure)
            return located.Error;
        if (await customers.CodeExistsAsync(customer.Code, ct))
            return CustomerErrors.CodeTaken;
        var unique = await CustomerRegistrations.CheckUniqueAsync(customers, customer, exceptId: null, ct);
        if (unique.IsFailure)
            return unique.Error;

        var saved = await customers.AddAsync(customer, ct);
        if (saved.IsFailure)
            return saved.Error;
        CustomerCreated(logger, customer.Id, customer.Code);
        return customer.Id;
    }

    [LoggerMessage(EventId = 7001, Level = LogLevel.Information, Message = "Customer {CustomerId} ({CustomerCode}) created")]
    private static partial void CustomerCreated(ILogger logger, Guid customerId, string customerCode);
}
