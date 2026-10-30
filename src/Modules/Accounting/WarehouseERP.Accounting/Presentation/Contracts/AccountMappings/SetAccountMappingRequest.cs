namespace WarehouseERP.Accounting.Presentation.Contracts.AccountMappings;

/// <param name="RowVersion">The mapping's version as read; required when the role is already mapped.</param>
public sealed record SetAccountMappingRequest(Guid AccountId, string? RowVersion);
