namespace WarehouseERP.Accounting.Application.Features.SourcePostings.RunSourceSync;

/// <param name="FullScan">Look for documents from the cutover again instead of from the last scans (finds any a scan missed).</param>
public sealed record RunSourceSyncCommand(bool FullScan);
