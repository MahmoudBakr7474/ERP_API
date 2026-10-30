namespace WarehouseERP.Accounting.Application.Features.Cutover.ActivateAccounting;

/// <param name="RowVersion">The settings' version the activating user read with the readiness checks.</param>
public sealed record ActivateAccountingCommand(string RowVersion);
