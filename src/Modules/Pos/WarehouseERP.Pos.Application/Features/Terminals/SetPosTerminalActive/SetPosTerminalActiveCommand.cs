namespace WarehouseERP.Pos.Application.Features.Terminals.SetPosTerminalActive;

public sealed record SetPosTerminalActiveCommand(Guid TerminalId, bool IsActive);
