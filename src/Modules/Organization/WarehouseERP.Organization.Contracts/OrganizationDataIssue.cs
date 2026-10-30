namespace WarehouseERP.Organization.Contracts;

/// <summary>
/// A configuration problem the master-data quality report finds. Only states the database and the domain rules still
/// allow are reported; what they already make impossible (a selling transit warehouse, two default locations) is not.
/// </summary>
public enum OrganizationDataIssue : byte
{
    /// <summary>The company has no tax registration number, which tax invoices print.</summary>
    CompanyMissingTaxNumber = 1,

    /// <summary>The company has no commercial registration number.</summary>
    CompanyMissingCommercialRegistration = 2,

    /// <summary>The company's address has no street line or no city.</summary>
    CompanyMissingAddress = 3,

    /// <summary>The company has neither a phone number nor an e-mail address.</summary>
    CompanyMissingContact = 4,

    /// <summary>English documents fall back to the Arabic name (company, branch, warehouse or storage location).</summary>
    MissingEnglishName = 5,

    /// <summary>An active branch with no active warehouse: nothing can be stocked, bought or sold there.</summary>
    BranchWithoutActiveWarehouse = 6,

    /// <summary>An active warehouse in an inactive branch (the screens refuse it; older data may still hold it).</summary>
    ActiveWarehouseInInactiveBranch = 7,

    /// <summary>A warehouse without a default location, where stock goes when no location is named.</summary>
    NoDefaultLocation = 8,

    /// <summary>A warehouse whose default location is inactive.</summary>
    InactiveDefaultLocation = 9,

    /// <summary>
    /// An active branch with active physical warehouses but not exactly one active transit warehouse, so every transfer
    /// out of it must name its transit warehouse.
    /// </summary>
    NoDefaultTransitWarehouse = 10,
}
