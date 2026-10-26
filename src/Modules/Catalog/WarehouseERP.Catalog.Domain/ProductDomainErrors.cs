using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Catalog.Domain;

public static class ProductDomainErrors
{
    public static readonly Error TrackingNeedsStockItem = Error.BusinessRule(
        "Catalog.Product.TrackingNeedsStockItem", "Only stock items can be tracked by batch or serial number.");
}
