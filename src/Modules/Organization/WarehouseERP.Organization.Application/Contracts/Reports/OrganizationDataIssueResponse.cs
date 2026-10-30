using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Reports;

/// <summary>One configuration problem of one record. Ids lead to the record's screen; codes and names describe it.</summary>
/// <param name="Code">The record's own code (null for the company row of a company issue).</param>
/// <param name="Detail">What makes it a problem, when the issue alone doesn't say (a transit warehouse count).</param>
public sealed record OrganizationDataIssueResponse(
    OrganizationDataIssue Issue, OrganizationEntity Entity, Guid? EntityId, string? Code, string? NameAr, string? NameEn,
    Guid? BranchId, string? BranchCode, Guid? WarehouseId, string? WarehouseCode, string? Detail);
