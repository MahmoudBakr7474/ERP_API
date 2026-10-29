using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Domain;

public static class SalesSettingsDomainErrors
{
    public static readonly Error DiscountThresholdInvalid = Error.BusinessRule(
        "Sales.Settings.DiscountThresholdInvalid", "The discount approval threshold runs from 0 to below 100%, with at most 4 decimals.");

    public static readonly Error CreditHoldPolicyInvalid = Error.BusinessRule(
        "Sales.Settings.CreditHoldPolicyInvalid", "The credit hold policy is unknown.");

    public static readonly Error DefaultVatRateInvalid = Error.BusinessRule(
        "Sales.Settings.DefaultVatRateInvalid", "The default VAT rate is above 0 and up to 100%, with at most 4 decimals.");

    public static readonly Error QuotationValidityInvalid = Error.BusinessRule(
        "Sales.Settings.QuotationValidityInvalid", "Quotations stay valid for 1 to 365 days.");
}
