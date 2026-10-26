using WarehouseERP.BuildingBlocks.Application.Security;

namespace WarehouseERP.IntegrationTests;

internal sealed class TestCurrentUser(Guid? userId, bool allWarehouses, params Guid[] warehouses) : ICurrentUser
{
    public static TestCurrentUser AllAccess() => new(Guid.Parse("11111111-1111-1111-1111-111111111111"), allWarehouses: true);

    public Guid? UserId { get; } = userId;

    public bool IsAuthenticated => UserId.HasValue;

    public bool HasPermission(string permission) => IsAuthenticated;

    public bool HasAllWarehouses { get; } = allWarehouses;

    public IReadOnlySet<Guid> WarehouseIds { get; } = warehouses.ToHashSet();
}
