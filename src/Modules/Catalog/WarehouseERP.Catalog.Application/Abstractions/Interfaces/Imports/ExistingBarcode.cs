using WarehouseERP.Catalog.Contracts;

namespace WarehouseERP.Catalog.Application.Abstractions.Interfaces.Imports;

/// <summary>A barcode value already in use (active or retired: values are never reused), and where.</summary>
public sealed record ExistingBarcode(string Barcode, string Sku, string UnitCode, BarcodeType Type);
