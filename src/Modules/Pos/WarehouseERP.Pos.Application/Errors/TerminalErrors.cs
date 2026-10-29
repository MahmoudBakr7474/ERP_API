using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Errors;

// Terminals and what they refer to in Organization; a terminal's own invariants are PosTerminalDomainErrors.
public static class TerminalErrors
{
    public static readonly Error NotFound = Error.NotFound("Pos.Terminal.NotFound", "The terminal does not exist.");

    public static readonly Error CodeTaken = Error.Conflict("Pos.Terminal.CodeTaken", "Another terminal already has this code.");

    public static readonly Error NotActive = Error.BusinessRule("Pos.Terminal.NotActive", "The terminal is inactive.");

    public static readonly Error HasOpenShift = Error.BusinessRule(
        "Pos.Terminal.HasOpenShift", "Close the terminal's open shift first.");

    public static readonly Error WarehouseNotFound = Error.BusinessRule(
        "Pos.Terminal.WarehouseNotFound", "The warehouse does not exist.");

    public static readonly Error WarehouseNotActive = Error.BusinessRule(
        "Pos.Terminal.WarehouseNotActive", "The warehouse is inactive.");

    public static readonly Error WarehouseNotSelling = Error.BusinessRule(
        "Pos.Terminal.WarehouseNotSelling", "A terminal sells from a physical warehouse that allows selling.");

    public static readonly Error LocationNotFound = Error.BusinessRule(
        "Pos.Terminal.LocationNotFound", "The selling location is not a location of the terminal's warehouse.");

    public static readonly Error LocationNotActive = Error.BusinessRule(
        "Pos.Terminal.LocationNotActive", "The selling location is inactive.");
}
