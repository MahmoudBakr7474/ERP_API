namespace WarehouseERP.Organization.Application.Features.Geography.SetGovernorateActive;

public sealed record SetGovernorateActiveCommand(Guid GovernorateId, bool IsActive);
