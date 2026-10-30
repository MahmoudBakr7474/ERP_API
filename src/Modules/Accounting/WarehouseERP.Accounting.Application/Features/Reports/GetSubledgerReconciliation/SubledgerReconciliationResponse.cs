namespace WarehouseERP.Accounting.Application.Features.Reports.GetSubledgerReconciliation;

public sealed record SubledgerReconciliationResponse(DateOnly AsOfDate, IReadOnlyList<SubledgerAccountResponse> Accounts, bool IsReconciled);
