using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.Purchasing.Application.Shared;
using WarehouseERP.Purchasing.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.UpdateSupplier;

// Only from the version the editor read (409 otherwise). The code is fixed; orders already placed keep their supplier
// and their own payment terms. Returns the new row version.
public sealed partial class UpdateSupplierHandler(ISupplierRepository suppliers, ILogger<UpdateSupplierHandler> logger)
    : ICommandHandler<UpdateSupplierCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateSupplierCommand command, CancellationToken ct)
    {
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var supplier = await suppliers.GetForUpdateAsync(command.SupplierId, expected, ct);
        if (supplier is null)
            return SupplierErrors.NotFound;

        var updated = supplier.Update(new SupplierDetails(
            command.NameAr, command.NameEn, command.CountryCode, command.TaxRegistrationNumber, command.CommercialRegistrationNumber,
            command.CommercialRegistry, command.PaymentTermDays, command.Phone, command.Email, command.Notes,
            SupplierRules.Contacts(command.Contacts), SupplierRules.Addresses(command.Addresses)));
        if (updated.IsFailure)
            return updated.Error;
        var unique = await SupplierRegistrations.CheckUniqueAsync(suppliers, supplier, supplier.Id, ct);
        if (unique.IsFailure)
            return unique.Error;
        var saved = await suppliers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        SupplierUpdated(logger, supplier.Id);
        return Convert.ToBase64String(supplier.RowVersion);
    }

    [LoggerMessage(EventId = 6002, Level = LogLevel.Information, Message = "Supplier {SupplierId} updated")]
    private static partial void SupplierUpdated(ILogger logger, Guid supplierId);
}
