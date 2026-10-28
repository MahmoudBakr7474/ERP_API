using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.CreateSupplier;

// One save point. The pre-checks give clear errors; the database's unique indexes still guard the code and the
// registrations when two requests race. A similar name is only a warning (FindSimilarSuppliers), never refused here.
// The country and the addresses' cities must be active entries of Organization's lookups.
public sealed partial class CreateSupplierHandler(
    ISupplierRepository suppliers, IGeographyDirectory geography, ILogger<CreateSupplierHandler> logger)
    : ICommandHandler<CreateSupplierCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateSupplierCommand command, CancellationToken ct)
    {
        var created = Supplier.Create(command.Code, new SupplierDetails(
            command.NameAr, command.NameEn, command.CountryCode, command.TaxRegistrationNumber, command.CommercialRegistrationNumber,
            command.CommercialRegistry, command.PaymentTermDays, command.Phone, command.Email, command.Notes,
            SupplierRules.Contacts(command.Contacts), SupplierRules.Addresses(command.Addresses)));
        if (created.IsFailure)
            return created.Error;
        var supplier = created.Value;
        var located = await SupplierGeography.CheckAsync(geography, current: null, command.CountryCode, command.Addresses, ct);
        if (located.IsFailure)
            return located.Error;
        if (await suppliers.CodeExistsAsync(supplier.Code, ct))
            return SupplierErrors.CodeTaken;
        var unique = await SupplierRegistrations.CheckUniqueAsync(suppliers, supplier, exceptId: null, ct);
        if (unique.IsFailure)
            return unique.Error;

        var saved = await suppliers.AddAsync(supplier, ct);
        if (saved.IsFailure)
            return saved.Error;

        SupplierCreated(logger, supplier.Id, supplier.Code);
        return supplier.Id;
    }

    [LoggerMessage(EventId = 6001, Level = LogLevel.Information, Message = "Supplier {SupplierId} ({SupplierCode}) created")]
    private static partial void SupplierCreated(ILogger logger, Guid supplierId, string supplierCode);
}
