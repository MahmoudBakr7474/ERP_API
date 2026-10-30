using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Application.Contracts.Accounts;

/// <summary>A posting role of an imported chart (or of the default chart) and the code of the posting account it uses.</summary>
public sealed record ChartRoleRow(PostingRole Role, string AccountCode);
