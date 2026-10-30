namespace WarehouseERP.Accounting.Application.Features.Accounts.SetAccountActive;

public sealed record SetAccountActiveCommand(Guid AccountId, bool IsActive);
