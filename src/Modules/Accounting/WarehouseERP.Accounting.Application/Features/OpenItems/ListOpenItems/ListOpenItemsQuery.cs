using WarehouseERP.Accounting.Contracts;
using WarehouseERP.BuildingBlocks.Application.Pagination;

namespace WarehouseERP.Accounting.Application.Features.OpenItems.ListOpenItems;

/// <param name="OpenOnly">Leave out items already fully allocated (the default).</param>
public sealed record ListOpenItemsQuery(int Page, int? PageSize, PartyType? PartyType, Guid? PartyId, Guid? AccountId, bool OpenOnly) : IPagedQuery;
