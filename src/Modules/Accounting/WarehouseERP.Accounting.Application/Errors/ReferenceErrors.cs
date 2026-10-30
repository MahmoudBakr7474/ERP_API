using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Accounting.Application.Errors;

public static class ReferenceErrors
{
    public static readonly Error CompanySetupRequired = Error.BusinessRule(
        "Accounting.Company.SetupRequired", "Set up the company first; its base currency is the ledger's currency.");
}
