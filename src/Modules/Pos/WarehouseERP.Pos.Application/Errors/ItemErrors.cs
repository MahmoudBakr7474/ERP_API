using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

// Scanning and searching items at a till.
public static class ItemErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Pos.Item.NotFound", "No sellable item has this barcode (it may be unknown or inactive).");
}
