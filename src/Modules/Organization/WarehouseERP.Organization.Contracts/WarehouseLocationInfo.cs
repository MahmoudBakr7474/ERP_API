namespace WarehouseERP.Organization.Contracts;

/// <summary>A warehouse location as other modules see it.</summary>
/// <param name="Id">Location id.</param>
/// <param name="WarehouseId">The warehouse it belongs to.</param>
/// <param name="Code">Code, unique within the warehouse.</param>
/// <param name="NameAr">Arabic name.</param>
/// <param name="IsDefault">True for the warehouse's DEFAULT location, where stock goes when no location is named.</param>
/// <param name="IsActive">False once retired.</param>
public sealed record WarehouseLocationInfo(Guid Id, Guid WarehouseId, string Code, string NameAr, bool IsDefault, bool IsActive);
