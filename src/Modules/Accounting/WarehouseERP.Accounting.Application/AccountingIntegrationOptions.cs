using System.ComponentModel.DataAnnotations;

namespace WarehouseERP.Accounting.Application;

/// <summary>
/// How the background worker journals operational documents once accounting is active (<c>Accounting:Integration</c>).
/// The same run is also available on demand (<c>POST /api/accounting/source-postings/run</c>).
/// </summary>
public sealed class AccountingIntegrationOptions
{
    public const string SectionName = "Accounting:Integration";

    /// <summary>Whether the worker runs; with it off, documents are journaled only on demand.</summary>
    public bool WorkerEnabled { get; init; } = true;

    /// <summary>How often the worker looks for new documents and retries failures that are due.</summary>
    [Range(typeof(TimeSpan), "00:00:10", "1.00:00:00")]
    public TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// How far before the end of the last scan the next one starts: a document commits a moment after its timestamp, so a
    /// scan may miss one still committing; the overlap finds it next time.
    /// </summary>
    [Range(typeof(TimeSpan), "00:01:00", "1.00:00:00")]
    public TimeSpan ScanOverlap { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>At most this many documents are journaled per run; the rest wait for the next one.</summary>
    [Range(1, 5000)]
    public int BatchSize { get; init; } = 500;
}
