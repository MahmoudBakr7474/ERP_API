using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Purchasing.Application.Errors;

public static class GoodsReceiptErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Purchasing.GoodsReceipt.NotFound", "The goods receipt does not exist.");
}
