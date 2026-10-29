namespace WarehouseERP.Pos.Application.Features.Settings.UpdatePosSettings;

/// <param name="MaxLineDiscountPercent">The largest line discount a till may give, 0 to 100.</param>
/// <param name="ReturnWindowDays">Days after the sale its goods may come back, 0 to 365.</param>
public sealed record UpdatePosSettingsCommand(decimal MaxLineDiscountPercent, int ReturnWindowDays, string RowVersion);
