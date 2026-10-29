using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Domain;

/// <summary>What a returned quantity takes back from its sale line, and the condition it came back in.</summary>
public sealed record PosReturnLineInput(ReturnShare Share, PosReturnCondition Condition);
