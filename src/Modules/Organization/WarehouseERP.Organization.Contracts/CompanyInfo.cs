namespace WarehouseERP.Organization.Contracts;

/// <summary>The company as other modules see it.</summary>
/// <param name="Id">Company id.</param>
/// <param name="Code">Company code.</param>
/// <param name="NameAr">Arabic name.</param>
/// <param name="BaseCurrencyCode">ISO currency of documents, balances and costs.</param>
/// <param name="TimeZoneId">IANA or Windows time zone id; it decides business dates such as the fiscal year.</param>
public sealed record CompanyInfo(Guid Id, string Code, string NameAr, string BaseCurrencyCode, string TimeZoneId);
