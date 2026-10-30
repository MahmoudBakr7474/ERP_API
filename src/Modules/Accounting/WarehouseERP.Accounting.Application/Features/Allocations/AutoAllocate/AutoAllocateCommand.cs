using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Features.Allocations.AutoAllocate;

/// <summary>Settles a party's open debit and credit items against each other, oldest first, on each control account.</summary>
public sealed record AutoAllocateCommand(PartyType PartyType, Guid PartyId);
