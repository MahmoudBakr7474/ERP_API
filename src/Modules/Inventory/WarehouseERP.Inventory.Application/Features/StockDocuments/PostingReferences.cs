using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Inventory.Application.Features.StockDocuments;

/// <summary>What a posting checked in Catalog and Organization, kept to explain a shortage by SKU and location code.</summary>
public sealed record PostingReferences(
    WarehouseInfo Warehouse, IReadOnlyDictionary<Guid, ProductInfo> Products, IReadOnlyDictionary<Guid, WarehouseLocationInfo> Locations);
