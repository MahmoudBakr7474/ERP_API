namespace WarehouseERP.Accounting.Application.Features.CashAccounts.SetCashAccountActive;

public sealed record SetCashAccountActiveCommand(Guid CashAccountId, bool IsActive);
