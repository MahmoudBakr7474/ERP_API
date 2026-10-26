namespace WarehouseERP.Organization.Contracts;

/// <summary>What a warehouse is for.</summary>
public enum WarehouseType : byte
{
    /// <summary>A real store of goods; may sell when it allows selling.</summary>
    Physical = 1,

    /// <summary>Goods between two warehouses during a transfer; never sells.</summary>
    Transit = 2,
}
