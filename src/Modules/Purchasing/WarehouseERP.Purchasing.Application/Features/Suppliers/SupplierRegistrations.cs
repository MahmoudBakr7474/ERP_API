using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers;

// A registration is unique within the jurisdiction that issues it: the tax number per country, the commercial
// registration per country and registry office. The pre-check gives a clear error; the database's filtered unique
// indexes still decide when two saves race.
internal static class SupplierRegistrations
{
    public static async Task<Result> CheckUniqueAsync(ISupplierRepository suppliers, Supplier supplier, Guid? exceptId, CancellationToken ct)
    {
        if (supplier.TaxRegistrationNumber is { } taxNumber
            && await suppliers.TaxNumberTakenAsync(supplier.CountryCode, taxNumber, exceptId, ct))
            return SupplierErrors.TaxNumberTaken;
        if (supplier.CommercialRegistrationNumber is { } registration
            && await suppliers.CommercialRegistrationTakenAsync(supplier.CountryCode, supplier.CommercialRegistryKey!, registration, exceptId, ct))
            return SupplierErrors.CommercialRegistrationTaken;
        return Result.Success();
    }
}
