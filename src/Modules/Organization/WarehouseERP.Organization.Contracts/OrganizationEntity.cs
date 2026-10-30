namespace WarehouseERP.Organization.Contracts;

/// <summary>The kind of record a master-data quality issue is about.</summary>
public enum OrganizationEntity : byte
{
    /// <summary>The company itself.</summary>
    Company = 1,

    /// <summary>A branch.</summary>
    Branch = 2,

    /// <summary>A warehouse.</summary>
    Warehouse = 3,

    /// <summary>A warehouse's storage location.</summary>
    Location = 4,
}
