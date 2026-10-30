using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Imports;

/// <summary>
/// What a location import would do, without changing anything. Confirm it by sending the same file and mode with
/// <paramref name="PreviewToken"/>: the token changes when the file, the mode or any row's outcome changes, so a
/// commit never does something other than what was previewed.
/// </summary>
/// <param name="CanCommit">No issue at all, and at least one row to create or update.</param>
/// <param name="Issues">At most 1,000 issues; <paramref name="IssuesTruncated"/> tells there are more (the error report has all).</param>
public sealed record LocationImportPreviewResponse(
    LocationImportMode Mode, string PreviewToken, bool CanCommit, LocationImportCounts Totals,
    IReadOnlyList<LocationImportRowResult> Rows, IReadOnlyList<LocationImportIssue> Issues, int IssueCount, bool IssuesTruncated);
