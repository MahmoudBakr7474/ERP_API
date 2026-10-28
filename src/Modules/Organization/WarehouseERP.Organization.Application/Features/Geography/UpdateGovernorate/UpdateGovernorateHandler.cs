using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Geography;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.Organization.Application.Shared;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Geography.UpdateGovernorate;

// Company-wide users only; the country is fixed. Returns the new row version for the caller's next update.
public sealed partial class UpdateGovernorateHandler(
    IGeographyRepository geography, ICurrentUser currentUser, ILogger<UpdateGovernorateHandler> logger)
    : ICommandHandler<UpdateGovernorateCommand, string>
{
    public async Task<Result<string>> HandleAsync(UpdateGovernorateCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        if (!RowVersions.TryParse(command.RowVersion, out var expected))
            return RowVersionErrors.Invalid;
        var governorate = await geography.GetGovernorateAsync(command.GovernorateId, expected, ct);
        if (governorate is null)
            return GeographyErrors.GovernorateNotFound;

        governorate.Rename(command.NameAr, command.NameEn);
        var saved = await geography.SaveChangesAsync(ct);
        if (saved.IsFailure)
            return saved.Error;

        GovernorateRenamed(logger, governorate.Id);
        return Convert.ToBase64String(governorate.RowVersion);
    }

    [LoggerMessage(EventId = 2024, Level = LogLevel.Information, Message = "Governorate {GovernorateId} renamed")]
    private static partial void GovernorateRenamed(ILogger logger, Guid governorateId);
}
