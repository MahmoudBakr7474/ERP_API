namespace WarehouseERP.Pos.Application.Features.Settings.GetPosSettings;

/// <summary>The POS policies: the largest line discount a till may give and the return window in days.</summary>
public sealed record PosSettingsResponse(decimal MaxLineDiscountPercent, int ReturnWindowDays, DateTime? UpdatedAtUtc, string RowVersion);
