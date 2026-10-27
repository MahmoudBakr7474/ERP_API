namespace WarehouseERP.Identity.Application.Contracts.UserAccess;

/// <param name="Revision">Send it back when changing the scope; it grows with every change.</param>
public sealed record UserAccessResponse(bool IsCompanyWide, IReadOnlyList<Guid> BranchIds, IReadOnlyList<Guid> WarehouseIds, int Revision);
