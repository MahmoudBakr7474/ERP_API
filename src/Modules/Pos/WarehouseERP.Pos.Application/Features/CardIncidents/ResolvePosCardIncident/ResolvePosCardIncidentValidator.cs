using FluentValidation;
using WarehouseERP.Pos.Application.Shared;
using WarehouseERP.Pos.Domain;

namespace WarehouseERP.Pos.Application.Features.CardIncidents.ResolvePosCardIncident;

public sealed class ResolvePosCardIncidentValidator : AbstractValidator<ResolvePosCardIncidentCommand>
{
    public ResolvePosCardIncidentValidator()
    {
        RuleFor(x => x.Note).NotEmpty().MaximumLength(PosCardIncident.MaxNoteLength);
        RuleFor(x => x.RowVersion).MustBeRowVersion();
    }
}
