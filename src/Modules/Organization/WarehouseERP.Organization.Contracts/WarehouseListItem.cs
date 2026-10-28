namespace WarehouseERP.Organization.Contracts;

/// <summary>A warehouse with its names and its branch's code, for pickers.</summary>
public sealed record WarehouseListItem(Guid Id, Guid BranchId, string BranchCode, string Code, string NameAr, string? NameEn);
