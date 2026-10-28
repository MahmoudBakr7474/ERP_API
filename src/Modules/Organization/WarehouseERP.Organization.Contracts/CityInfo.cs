namespace WarehouseERP.Organization.Contracts;

/// <summary>A city with the governorate and country it belongs to, as other modules see it.</summary>
/// <param name="Id">City id.</param>
/// <param name="NameAr">Arabic name of the city.</param>
/// <param name="NameEn">English name of the city, when there is one.</param>
/// <param name="IsActive">The city's own flag; see <see cref="IsUsable"/> for new addresses.</param>
/// <param name="GovernorateId">Its governorate, fixed once the city is created.</param>
/// <param name="GovernorateNameAr">Arabic name of the governorate.</param>
/// <param name="GovernorateNameEn">English name of the governorate, when there is one.</param>
/// <param name="GovernorateIsActive">The governorate's flag.</param>
/// <param name="CountryCode">ISO 3166 alpha-2 code of the governorate's country.</param>
/// <param name="CountryNameAr">Arabic name of the country.</param>
/// <param name="CountryNameEn">English name of the country, when there is one.</param>
/// <param name="CountryIsActive">The country's flag.</param>
public sealed record CityInfo(
    Guid Id, string NameAr, string? NameEn, bool IsActive,
    Guid GovernorateId, string GovernorateNameAr, string? GovernorateNameEn, bool GovernorateIsActive,
    string CountryCode, string CountryNameAr, string? CountryNameEn, bool CountryIsActive)
{
    /// <summary>A new address may name it: the city, its governorate and its country are all active.</summary>
    public bool IsUsable => IsActive && GovernorateIsActive && CountryIsActive;
}
