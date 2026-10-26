namespace WarehouseERP.Identity.Domain;

public sealed class UserWarehouseGrant
{
    private UserWarehouseGrant() { } // EF Core

    internal UserWarehouseGrant(Guid warehouseId) => WarehouseId = warehouseId;

    public Guid UserAccessId { get; private set; }
    public Guid WarehouseId { get; private set; }
}
