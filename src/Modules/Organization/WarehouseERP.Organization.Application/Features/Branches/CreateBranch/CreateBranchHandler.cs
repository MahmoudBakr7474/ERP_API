using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;
using WarehouseERP.BuildingBlocks.Application.Security;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Branches;
using WarehouseERP.Organization.Application.Abstractions.Interfaces.Companies;
using WarehouseERP.Organization.Application.Errors;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Organization.Application.Features.Branches.CreateBranch;

// Company-wide users only; the company must be set up first. One save point (IBranchRepository.AddAsync); the
// pre-check gives a clear error and the database still guards the code if two requests race.
public sealed partial class CreateBranchHandler(
    ICompanyRepository companies, IBranchRepository branches, ICurrentUser currentUser, ILogger<CreateBranchHandler> logger)
    : ICommandHandler<CreateBranchCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateBranchCommand command, CancellationToken ct)
    {
        if (!currentUser.HasAllWarehouses)
            return CompanyErrors.CompanyWideAccessRequired;
        var company = await companies.GetForUpdateAsync(expectedRowVersion: null, ct);
        if (company is null)
            return CompanyErrors.SetupRequired;
        if (await branches.CodeExistsAsync(company.Id, command.Code.Trim(), ct))
            return BranchErrors.CodeTaken;

        var branch = company.AddBranch(command.Code, command.NameAr, command.NameEn);
        var saved = await branches.AddAsync(branch, ct);
        if (saved.IsFailure)
            return saved.Error;

        BranchCreated(logger, branch.Id, branch.Code);
        return branch.Id;
    }

    [LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Branch {BranchId} ({BranchCode}) created")]
    private static partial void BranchCreated(ILogger logger, Guid branchId, string branchCode);
}
