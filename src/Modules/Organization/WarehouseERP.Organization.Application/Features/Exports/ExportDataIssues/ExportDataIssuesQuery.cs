using WarehouseERP.Organization.Contracts;

namespace WarehouseERP.Organization.Application.Features.Exports.ExportDataIssues;

/// <param name="MaxRows">Set by the server from the format's limit (Documents options), never by the client.</param>
public sealed record ExportDataIssuesQuery(OrganizationDataIssue? Issue, int MaxRows);
