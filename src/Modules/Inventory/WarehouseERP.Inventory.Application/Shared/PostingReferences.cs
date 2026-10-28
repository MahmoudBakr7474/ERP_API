using WarehouseERP.Catalog.Contracts;
using WarehouseERP.Inventory.Domain;
using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Inventory.Application.Shared;

/// <summary>What a posting checked in Catalog and Organization, kept to explain a shortage by SKU and location code.</summary>
public sealed record PostingReferences(
    IReadOnlyDictionary<Guid, WarehouseInfo> Warehouses, IReadOnlyDictionary<Guid, ProductInfo> Products,
    IReadOnlyDictionary<Guid, WarehouseLocationInfo> Locations, IReadOnlyList<StockPlace> Places);
