using WarehouseERP.Sales.Application.Abstractions.Interfaces.Customers;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Features.Orders;

// An order keeps its delivery address as text taken from one of the customer's addresses (with the city and governorate
// names the lookups have now), so later edits of the customer never change it.
internal static class ShippingAddresses
{
    public static async Task<Result<string?>> ResolveAsync(ICustomerReadStore customers, Guid customerId, Guid? addressId, CancellationToken ct)
    {
        if (addressId is not { } id)
            return Result.Success<string?>(null);
        var customer = await customers.GetAsync(customerId, ct);
        var address = customer?.Addresses.SingleOrDefault(a => a.Id == id);
        if (address is null)
            return CustomerErrors.AddressNotFound;
        var parts = new[] { address.Line1, address.Line2, address.CityNameAr, address.GovernorateNameAr, address.PostalCode }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join("، ", parts);
    }
}
