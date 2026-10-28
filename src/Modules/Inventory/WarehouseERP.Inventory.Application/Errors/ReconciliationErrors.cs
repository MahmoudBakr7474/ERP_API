using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Inventory.Application.Errors;

public static class ReconciliationErrors
{
    public static readonly Error CompanyWideAccessRequired = Error.Forbidden(
        "Inventory.Reconciliation.CompanyWideAccessRequired", "Reconciliation covers every warehouse; it needs company-wide access.");
}
