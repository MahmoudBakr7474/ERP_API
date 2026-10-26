namespace WarehouseERP.Catalog.Contracts;

public enum ProductType : byte { StockItem = 1, NonStockItem = 2, Service = 3 }

public enum TrackingMode : byte { None = 0, Batch = 1, Serial = 2 }
