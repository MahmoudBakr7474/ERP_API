using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.Organization.Application.Abstractions;
using WarehouseERP.Organization.Contracts;
using WarehouseERP.Organization.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.CreateWarehouse;

// Owns the transaction: one save point (IWarehouseRepository.AddAsync) for the warehouse and its
// DEFAULT location. The pre-checks give clear errors; the database still guards the code if two
// requests race.
public sealed partial class CreateWarehouseHandler(IWarehouseRepository warehouses, ILogger<CreateWarehouseHandler> logger)
    : ICommandHandler<CreateWarehouseCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateWarehouseCommand command, CancellationToken ct)
    {
        if (!await warehouses.IsBranchActiveAsync(command.BranchId, ct))
            return OrganizationErrors.BranchNotActive;
        if (await warehouses.CodeExistsAsync(command.BranchId, command.Code.Trim(), ct))
            return OrganizationErrors.WarehouseCodeTaken;

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
