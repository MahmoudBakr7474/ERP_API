using FluentValidation;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Accounts.ImportChartOfAccounts;

public sealed class ImportChartOfAccountsValidator : AbstractValidator<ImportChartOfAccountsCommand>
{
    public ImportChartOfAccountsValidator()
    {
        RuleFor(x => x.Accounts).NotEmpty().Must(a => a.Count <= ChartBuilder.MaxRows)
            .WithMessage($"A chart has at most {ChartBuilder.MaxRows} accounts.");
        RuleForEach(x => x.Accounts).ChildRules(row =>
        {
            row.RuleFor(r => r.Code).NotEmpty().MaximumLength(Account.MaxCodeLength);
            row.RuleFor(r => r.NameAr).NotEmpty().MaximumLength(Account.MaxNameLength);
            row.RuleFor(r => r.NameEn).MaximumLength(Account.MaxNameLength);
            row.RuleFor(r => r.ParentCode).MaximumLength(Account.MaxCodeLength);
            row.RuleFor(r => r.Type).IsInEnum();
            row.RuleFor(r => r.Section).IsInEnum();
            row.RuleFor(r => r.PartyType).IsInEnum();
        });
        RuleForEach(x => x.Roles).ChildRules(role =>
        {
            role.RuleFor(r => r.Role).IsInEnum();
            role.RuleFor(r => r.AccountCode).NotEmpty().MaximumLength(Account.MaxCodeLength);
        }).When(x => x.Roles is not null);
        RuleFor(x => x.Roles).Must(r => r!.Select(x => x.Role).Distinct().Count() == r!.Count)
            .WithMessage("Each posting role is mapped once.").When(x => x.Roles is not null);
    }
}
