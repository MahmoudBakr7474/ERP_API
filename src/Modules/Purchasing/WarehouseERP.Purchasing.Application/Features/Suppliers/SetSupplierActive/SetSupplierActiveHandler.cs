using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Suppliers;
using WarehouseERP.Purchasing.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Features.Suppliers.SetSupplierActive;

// An inactive supplier takes no new orders and no approvals of orders still in draft or submitted; orders already
// approved are received, invoiced and returned as usual.
public sealed partial class SetSupplierActiveHandler(ISupplierRepository suppliers, ILogger<SetSupplierActiveHandler> logger)
    : ICommandHandler<SetSupplierActiveCommand, Done>
{
    public async Task<Result<Done>> HandleAsync(SetSupplierActiveCommand command, CancellationToken ct)
    {
        var supplier = await suppliers.GetForUpdateAsync(command.SupplierId, expectedRowVersion: null, ct);
        if (supplier is null)
            return SupplierErrors.NotFound;

        if (command.IsActive) supplier.Activate();
        else supplier.Deactivate();
        var saved = await suppliers.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        ActiveChanged(logger, supplier.Id, command.IsActive);
        return Done.Value;
    }

    [LoggerMessage(EventId = 6003, Level = LogLevel.Information, Message = "Supplier {SupplierId} active set to {IsActive}")]
    private static partial void ActiveChanged(ILogger logger, Guid supplierId, bool isActive);
}
