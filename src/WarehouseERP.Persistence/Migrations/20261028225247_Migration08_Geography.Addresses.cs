namespace WarehouseERP.Persistence.Migrations;

// Migration08 replaces a supplier address's free-text City, Region and CountryCode with CityId, keeping the data:
// each address (and each of its history versions) is matched to exactly one seeded city of its country, by Arabic or
// English name, and by governorate name when it had a region. If any address can't be matched, or a supplier's country
// isn't in the list, the migration stops with the list of them and changes nothing (fix the text, then run it again).
// The table is temporal, so the columns change in both tables with versioning off, as EF Core does it. "GO" separates
// batches: a batch can't use a column added earlier in the same batch.
public partial class Migration08_Geography
{
    private const string CheckSupplierCountries = """
DECLARE @unknown nvarchar(max) = (
    SELECT STRING_AGG(CONVERT(nvarchar(max), CONCAT(s.[Code], N' (', s.[CountryCode], N')')), N', ')
    FROM [purchasing].[Suppliers] s
    WHERE NOT EXISTS (SELECT 1 FROM [organization].[Countries] c WHERE c.[Code] = s.[CountryCode]));
IF @unknown IS NOT NULL
BEGIN
    DECLARE @message nvarchar(2048) = LEFT(N'Migration08: these suppliers have a country code that is not an ISO 3166 country: ' + @unknown, 2048);
    THROW 50008, @message, 1;
END
""";

    private const string AddressesToCities = """
ALTER TABLE [purchasing].[SupplierAddresses] SET (SYSTEM_VERSIONING = OFF);
ALTER TABLE [purchasing].[SupplierAddresses] ADD [CityId] uniqueidentifier NULL;
ALTER TABLE [history].[SupplierAddressesHistory] ADD [CityId] uniqueidentifier NULL;
GO
UPDATE a SET [CityId] = m.[CityId]
FROM [purchasing].[SupplierAddresses] a
CROSS APPLY (
    SELECT CASE WHEN COUNT(*) = 1 THEN MIN(CONVERT(char(36), c.[Id])) END AS [CityId]
    FROM [organization].[Cities] c
    JOIN [organization].[Governorates] g ON g.[Id] = c.[GovernorateId]
    JOIN [organization].[Countries] co ON co.[Id] = g.[CountryId]
    WHERE co.[Code] = a.[CountryCode]
      AND (c.[NameAr] = LTRIM(RTRIM(a.[City])) OR c.[NameEn] = LTRIM(RTRIM(a.[City])))
      AND (a.[Region] IS NULL OR g.[NameAr] = LTRIM(RTRIM(a.[Region])) OR g.[NameEn] = LTRIM(RTRIM(a.[Region])))) m;

UPDATE h SET [CityId] = COALESCE(m.[CityId], current_row.[CityId])
FROM [history].[SupplierAddressesHistory] h
CROSS APPLY (
    SELECT CASE WHEN COUNT(*) = 1 THEN MIN(CONVERT(char(36), c.[Id])) END AS [CityId]
    FROM [organization].[Cities] c
    JOIN [organization].[Governorates] g ON g.[Id] = c.[GovernorateId]
    JOIN [organization].[Countries] co ON co.[Id] = g.[CountryId]
    WHERE co.[Code] = h.[CountryCode]
      AND (c.[NameAr] = LTRIM(RTRIM(h.[City])) OR c.[NameEn] = LTRIM(RTRIM(h.[City])))
      AND (h.[Region] IS NULL OR g.[NameAr] = LTRIM(RTRIM(h.[Region])) OR g.[NameEn] = LTRIM(RTRIM(h.[Region])))) m
OUTER APPLY (SELECT a.[CityId] FROM [purchasing].[SupplierAddresses] a WHERE a.[Id] = h.[Id]) current_row;
GO
DECLARE @unmatched nvarchar(max) = (
    SELECT STRING_AGG(CONVERT(nvarchar(max), CONCAT(s.[Code], N': ', x.[City], N' / ', COALESCE(x.[Region], N'-'), N' (', x.[CountryCode], N')')), N'; ')
    FROM (SELECT [SupplierId], [City], [Region], [CountryCode] FROM [purchasing].[SupplierAddresses] WHERE [CityId] IS NULL
          UNION
          SELECT [SupplierId], [City], [Region], [CountryCode] FROM [history].[SupplierAddressesHistory] WHERE [CityId] IS NULL) x
    JOIN [purchasing].[Suppliers] s ON s.[Id] = x.[SupplierId]);
IF @unmatched IS NOT NULL
BEGIN
    DECLARE @message nvarchar(2048) = LEFT(
        N'Migration08: these supplier addresses (supplier: city / region (country)) match no seeded city, or several; '
        + N'correct the city or add the governorate as the region, then run the migration again: ' + @unmatched, 2048);
    THROW 50008, @message, 1;
END
GO
ALTER TABLE [purchasing].[SupplierAddresses] ALTER COLUMN [CityId] uniqueidentifier NOT NULL;
ALTER TABLE [history].[SupplierAddressesHistory] ALTER COLUMN [CityId] uniqueidentifier NOT NULL;
ALTER TABLE [purchasing].[SupplierAddresses] DROP COLUMN [City], [Region], [CountryCode];
ALTER TABLE [history].[SupplierAddressesHistory] DROP COLUMN [City], [Region], [CountryCode];
ALTER TABLE [purchasing].[SupplierAddresses] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[SupplierAddressesHistory]));
""";

    // Down: the names come back as text from the lookups before they are dropped.
    private const string CitiesToAddresses = """
ALTER TABLE [purchasing].[SupplierAddresses] SET (SYSTEM_VERSIONING = OFF);
ALTER TABLE [purchasing].[SupplierAddresses] ADD [City] nvarchar(100) NULL, [Region] nvarchar(100) NULL, [CountryCode] char(2) NULL;
ALTER TABLE [history].[SupplierAddressesHistory] ADD [City] nvarchar(100) NULL, [Region] nvarchar(100) NULL, [CountryCode] char(2) NULL;
GO
UPDATE a SET [City] = c.[NameAr], [Region] = g.[NameAr], [CountryCode] = co.[Code]
FROM [purchasing].[SupplierAddresses] a
JOIN [organization].[Cities] c ON c.[Id] = a.[CityId]
JOIN [organization].[Governorates] g ON g.[Id] = c.[GovernorateId]
JOIN [organization].[Countries] co ON co.[Id] = g.[CountryId];

UPDATE h SET [City] = c.[NameAr], [Region] = g.[NameAr], [CountryCode] = co.[Code]
FROM [history].[SupplierAddressesHistory] h
JOIN [organization].[Cities] c ON c.[Id] = h.[CityId]
JOIN [organization].[Governorates] g ON g.[Id] = c.[GovernorateId]
JOIN [organization].[Countries] co ON co.[Id] = g.[CountryId];
GO
ALTER TABLE [purchasing].[SupplierAddresses] ALTER COLUMN [City] nvarchar(100) NOT NULL;
ALTER TABLE [history].[SupplierAddressesHistory] ALTER COLUMN [City] nvarchar(100) NOT NULL;
ALTER TABLE [purchasing].[SupplierAddresses] ALTER COLUMN [CountryCode] char(2) NOT NULL;
ALTER TABLE [history].[SupplierAddressesHistory] ALTER COLUMN [CountryCode] char(2) NOT NULL;
ALTER TABLE [purchasing].[SupplierAddresses] DROP COLUMN [CityId];
ALTER TABLE [history].[SupplierAddressesHistory] DROP COLUMN [CityId];
ALTER TABLE [purchasing].[SupplierAddresses] SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [history].[SupplierAddressesHistory]));
""";
}
