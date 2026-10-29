using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

public static class PosSettingsDomainErrors
{
    public static readonly Error MaxDiscountInvalid = Error.BusinessRule(
        "Pos.Settings.MaxDiscountInvalid", "The maximum line discount is 0 to 100 percent with at most 4 decimals.");

    public static readonly Error ReturnWindowInvalid = Error.BusinessRule(
        "Pos.Settings.ReturnWindowInvalid", $"The return window is 0 to {PosSettings.MaxReturnWindowDays} days.");
}
