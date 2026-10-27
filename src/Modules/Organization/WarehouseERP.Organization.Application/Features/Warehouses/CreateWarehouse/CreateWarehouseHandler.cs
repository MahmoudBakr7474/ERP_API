using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Warehouses;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Warehouses.CreateWarehouse;

// Owns the transaction: one save point (IWarehouseRepository.AddAsync) for the warehouse and its
// DEFAULT location. The pre-checks give clear errors; the database still guards the code if two
// requests race. Company-wide users only: the new warehouse is outside any narrower scope.
public sealed partial class CreateWarehouseHandler(
    IWarehouseRepository warehouses, ICurrentUser currentUser, ILogger<CreateWarehouseHandler> logger)
    : ICommandHandler<CreateWarehouseCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateWarehouseCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        if (!await warehouses.IsBranchActiveAsync(command.BranchId, ct))
            return BranchErrors.NotActive;
        if (await warehouses.CodeExistsAsync(command.BranchId, command.Code.Trim(), ct))
            return WarehouseErrors.CodeTaken;

        var created = Warehouse.Create(
            command.BranchId, command.Code, command.NameAr, command.NameEn, command.Type, command.AllowsSelling);
        if (created.IsFailure)
            return created.Error;

        var saved = await warehouses.AddAsync(created.Value, ct);
        if (saved.IsFailure)
            return saved.Error;

        var warehouse = created.Value;
        WarehouseCreated(logger, warehouse.Id, warehouse.BranchId, warehouse.Code, warehouse.WarehouseType);
        return warehouse.Id;
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Warehouse {WarehouseId} ({WarehouseCode}, {WarehouseType}) created in branch {BranchId}")]
    private static partial void WarehouseCreated(ILogger logger, Guid warehouseId, Guid branchId, string warehouseCode, WarehouseType warehouseType);
}
