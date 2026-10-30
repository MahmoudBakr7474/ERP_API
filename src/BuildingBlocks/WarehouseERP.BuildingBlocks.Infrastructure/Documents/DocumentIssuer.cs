namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// The company a document is issued by, as configured in Organization. Separate from the application's brand: the
/// logo is the product's (or a configured one), the names and tax number are the company's own and never invented.
/// </summary>
public sealed record DocumentIssuer(string NameAr, string? NameEn, string? TaxRegistrationNumber, string TimeZoneId);
