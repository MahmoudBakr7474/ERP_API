using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Errors;

// Countries, governorates and cities. A command that names a missing or inactive parent breaks a rule of the request
// (422); 404 is kept for the record the URL addresses.
public static class GeographyErrors
{
    public static readonly Error CountryNotFound = Error.NotFound(
        "Organization.Country.NotFound", "The country does not exist.");

    public static readonly Error CountryCodeTaken = Error.Conflict(
        "Organization.Country.CodeTaken", "Another country already uses this code.");

    public static readonly Error CountryNameTaken = Error.Conflict(
        "Organization.Country.NameTaken", "Another country already has this Arabic name.");

    public static readonly Error CountryNotActive = Error.BusinessRule(
        "Organization.Country.NotActive", "The country does not exist or is inactive.");

    public static readonly Error GovernorateNotFound = Error.NotFound(
        "Organization.Governorate.NotFound", "The governorate does not exist.");

    public static readonly Error GovernorateNameTaken = Error.Conflict(
        "Organization.Governorate.NameTaken", "The country already has a governorate with this Arabic name.");

    public static readonly Error GovernorateNotActive = Error.BusinessRule(
        "Organization.Governorate.NotActive", "The governorate does not exist or is inactive.");

    public static readonly Error CityNotFound = Error.NotFound(
        "Organization.City.NotFound", "The city does not exist.");

    public static readonly Error CityNameTaken = Error.Conflict(
        "Organization.City.NameTaken", "The governorate already has a city with this Arabic name.");
}
