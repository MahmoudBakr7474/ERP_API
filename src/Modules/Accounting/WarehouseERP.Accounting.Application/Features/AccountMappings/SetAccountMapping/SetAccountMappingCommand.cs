using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.AccountMappings.SetAccountMapping;

/// <param name="RowVersion">The mapping's version as read; required when the role is already mapped, ignored otherwise.</param>
public sealed record SetAccountMappingCommand(PostingRole Role, Guid AccountId, string? RowVersion);
