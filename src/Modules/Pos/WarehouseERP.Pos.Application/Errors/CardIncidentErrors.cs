using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

public static class CardIncidentErrors
{
    public static readonly Error NotFound = Error.NotFound("Pos.CardIncident.NotFound", "The card incident does not exist.");
}
