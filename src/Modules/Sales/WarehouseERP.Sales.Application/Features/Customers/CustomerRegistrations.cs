using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Customers;

// A registration is unique within the jurisdiction that issues it: the tax number per country, the commercial
// registration per country and registry office. The pre-check gives a clear error; the database's filtered unique
// indexes still decide when two saves race.
internal static class CustomerRegistrations
{
    public static async Task<Result> CheckUniqueAsync(ICustomerRepository customers, Customer customer, Guid? exceptId, CancellationToken ct)
    {
        if (customer.TaxRegistrationNumber is { } taxNumber
            && await customers.TaxNumberTakenAsync(customer.CountryCode, taxNumber, exceptId, ct))
            return CustomerErrors.TaxNumberTaken;
        if (customer.CommercialRegistrationNumber is { } registration
            && await customers.CommercialRegistrationTakenAsync(customer.CountryCode, customer.CommercialRegistryKey!, registration, exceptId, ct))
            return CustomerErrors.CommercialRegistrationTaken;
        return Result.Success();
    }
}
