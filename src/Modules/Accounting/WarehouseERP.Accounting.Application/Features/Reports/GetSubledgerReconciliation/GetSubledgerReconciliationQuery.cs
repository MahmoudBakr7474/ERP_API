namespace WarehouseERP.Accounting.Application.Features.Reports.GetSubledgerReconciliation;

/// <summary>Each customer and supplier control account against its open items at the end of a date.</summary>
public sealed record GetSubledgerReconciliationQuery(DateOnly AsOfDate);
