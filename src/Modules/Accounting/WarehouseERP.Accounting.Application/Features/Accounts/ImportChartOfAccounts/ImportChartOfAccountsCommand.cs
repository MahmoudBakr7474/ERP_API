using WarehouseERP.Accounting.Application.Contracts.Accounts;

namespace WarehouseERP.Accounting.Application.Features.Accounts.ImportChartOfAccounts;

/// <summary>An existing approved chart, imported whole into an empty chart.</summary>
/// <param name="Roles">Optional: posting roles to map, each to the code of one imported posting account.</param>
public sealed record ImportChartOfAccountsCommand(IReadOnlyList<ChartAccountRow> Accounts, IReadOnlyList<ChartRoleRow>? Roles);
