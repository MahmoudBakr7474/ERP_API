using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Accounts;

/// <param name="Search">Part of the code or of either name.</param>
/// <param name="ParentId">Only the direct children of this account.</param>
public sealed record AccountFilter(string? Search, AccountType? Type, bool? IsActive, bool? IsPosting, Guid? ParentId);
