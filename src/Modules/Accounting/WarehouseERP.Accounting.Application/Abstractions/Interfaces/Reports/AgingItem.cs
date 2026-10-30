using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Abstractions.Interfaces.Reports;

/// <summary>An open item as of a date: what was still open then (signed like its amount) and when it falls due.</summary>
public sealed record AgingItem(PartyType PartyType, Guid PartyId, Guid AccountId, DateOnly DueDate, decimal OpenAmount);
