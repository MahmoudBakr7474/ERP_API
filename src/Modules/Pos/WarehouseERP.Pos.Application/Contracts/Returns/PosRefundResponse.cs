using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Application.Contracts.Returns;

public sealed record PosRefundResponse(
    int LineNumber, PosPaymentMethod Method, PosPaymentStatus Status, decimal Amount, string? Reference, string? CardLast4);
