namespace WarehouseERP.Identity.Presentation.Contracts.Users;

/// <param name="Revision">From GET user (access.revision); refused (409 Common.ConcurrencyConflict) if the scope changed since.</param>
public sealed record SetAccessRequest(bool IsCompanyWide, IReadOnlyList<Guid>? BranchIds, IReadOnlyList<Guid>? WarehouseIds, int Revision);
