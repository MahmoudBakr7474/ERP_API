using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Errors;

public static class CompanyErrors
{
    public static readonly Error NotConfigured = Error.NotFound(
        "Organization.Company.NotConfigured", "The company has not been set up yet.");

    public static readonly Error AlreadyConfigured = Error.Conflict(
        "Organization.Company.AlreadyConfigured", "The company is already set up; change its settings instead.");

    // Creating a branch doesn't address the company, it needs it to exist: a rule of the request (422), not 404.
    public static readonly Error SetupRequired = Error.BusinessRule(
        "Organization.Company.SetupRequired", "Set up the company before creating branches.");

    // The company's settings and its structure (branches, new warehouses) belong to company-wide users.
    public static readonly Error CompanyWideAccessRequired = Error.Forbidden(
        "Organization.Company.CompanyWideAccessRequired",
        "Only a user with company-wide access can change the company or its branches, or create warehouses.");

    public static readonly Error CityNotUsable = Error.BusinessRule(
        "Organization.Company.CityNotUsable", "The city does not exist, or it, its governorate or its country is inactive.");
}
