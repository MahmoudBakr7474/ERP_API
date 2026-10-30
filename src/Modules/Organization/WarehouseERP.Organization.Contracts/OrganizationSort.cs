namespace WarehouseERP.Organization.Contracts;

/// <summary>What a branch, warehouse or location list (and its export) is ordered by; ascending unless "descending".</summary>
public enum OrganizationSort : byte
{
    /// <summary>The code (the default).</summary>
    Code = 1,

    /// <summary>The Arabic name.</summary>
    NameAr = 2,

    /// <summary>The English name; records without one come first.</summary>
    NameEn = 3,
}
