using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Domain;

public static class PosCardIncidentDomainErrors
{
    public static readonly Error NotOpen = Error.BusinessRule(
        "Pos.CardIncident.NotOpen", "This card incident is already settled.");

    public static readonly Error NoteRequired = Error.BusinessRule(
        "Pos.CardIncident.NoteRequired", $"Say how the card charge was settled, in at most {PosCardIncident.MaxNoteLength} characters.");
}
