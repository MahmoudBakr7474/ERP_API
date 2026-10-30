using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.OpenItems;

/// <param name="OpenOnly">Leave out items already fully allocated.</param>
public sealed record OpenItemFilter(PartyType? PartyType, Guid? PartyId, Guid? AccountId, bool OpenOnly);
