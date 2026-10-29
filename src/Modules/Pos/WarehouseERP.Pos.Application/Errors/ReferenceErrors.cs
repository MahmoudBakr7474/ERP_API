using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

public static class ReferenceErrors
{
    public static readonly Error CompanySetupRequired = Error.BusinessRule(
        "Pos.Reference.CompanySetupRequired", "Set up the company before selling.");
}
