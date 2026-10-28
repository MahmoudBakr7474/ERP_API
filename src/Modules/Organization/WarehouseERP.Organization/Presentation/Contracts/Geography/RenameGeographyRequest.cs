namespace WarehouseERP.Organization.Presentation.Contracts.Geography;

/// <summary>New names for a country, governorate or city; the record itself is named by the URL.</summary>
public sealed record RenameGeographyRequest(string NameAr, string? NameEn, string RowVersion);
