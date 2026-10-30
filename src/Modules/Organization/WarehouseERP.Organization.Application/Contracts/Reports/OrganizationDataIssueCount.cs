using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Contracts.Reports;

public sealed record OrganizationDataIssueCount(OrganizationDataIssue Issue, int Count);
