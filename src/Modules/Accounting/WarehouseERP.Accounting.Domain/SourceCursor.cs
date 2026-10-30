namespace WarehouseERP.Accounting.Domain;

/// <summary>
/// How far Accounting has looked for posted documents of one module. Each scan starts a little before the last one ended
/// (documents commit a moment after their timestamp), and the documents' accounting identity keeps the overlap from
/// counting anything twice.
/// </summary>
public sealed class SourceCursor
{
    public const int MaxModuleLength = 20;

    private SourceCursor() { } // EF Core

    public string Module { get; private set; } = null!;
    public DateTime ScannedToUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public static SourceCursor Start(string module, DateTime scannedToUtc, DateTime nowUtc) =>
        new() { Module = module, ScannedToUtc = scannedToUtc, UpdatedAtUtc = nowUtc };

    public void Advance(DateTime scannedToUtc, DateTime nowUtc)
    {
        if (scannedToUtc > ScannedToUtc)
            ScannedToUtc = scannedToUtc;
        UpdatedAtUtc = nowUtc;
    }
}
