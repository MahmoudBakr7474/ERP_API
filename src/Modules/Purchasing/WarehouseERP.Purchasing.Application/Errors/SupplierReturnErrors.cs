using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

public static class SupplierReturnErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Purchasing.SupplierReturn.NotFound", "The supplier return does not exist.");

    public static readonly Error ReceiptNotFound = Error.BusinessRule(
        "Purchasing.SupplierReturn.ReceiptNotFound", "The goods receipt does not exist.");
}
