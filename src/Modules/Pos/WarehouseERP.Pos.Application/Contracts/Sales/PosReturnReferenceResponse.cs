namespace WarehouseERP.Pos.Application.Contracts.Sales;

/// <summary>A return made against the sale.</summary>
public sealed record PosReturnReferenceResponse(Guid Id, string Number, DateTime ReturnedAtUtc, decimal TotalAmount);
