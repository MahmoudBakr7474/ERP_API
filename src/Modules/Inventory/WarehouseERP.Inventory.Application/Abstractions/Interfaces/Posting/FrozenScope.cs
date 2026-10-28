namespace WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;

/// <param name="CountReference">CT-000123.</param>
public sealed record FrozenScope(Guid CountId, string CountReference);
