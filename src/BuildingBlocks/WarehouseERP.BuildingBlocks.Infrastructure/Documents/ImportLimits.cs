namespace WarehouseERP.BuildingBlocks.Infrastructure.Documents;

/// <summary>
/// What an uploaded table may hold. The XLSX limits stop "zip bombs": a small file that would expand to gigabytes.
/// </summary>
public sealed record ImportLimits(
    long MaxFileBytes = 5 * 1024 * 1024,
    int MaxRowsPerSheet = 5_000,
    int MaxColumns = 40,
    int MaxCellLength = 1_000,
    int MaxZipEntries = 200,
    long MaxUncompressedBytes = 100L * 1024 * 1024,
    int MaxCompressionRatio = 200);
