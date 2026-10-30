using System.Linq.Expressions;
using FluentValidation;
using WarehouseERP.Accounting.Application.Contracts.Journals;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Journals;

// The input shape of journal lines, shared by the use cases that take them; balance, amounts' decimals and what the
// accounts allow are the domain's and the line rules' business.
internal static class JournalRequestRules
{
    public static void AddLineRules<T>(this AbstractValidator<T> validator, Expression<Func<T, IEnumerable<JournalLineRequest>?>> lines, bool required)
    {
        validator.RuleFor(lines)
            .Must(l => l is not null ? l.Count() is >= 2 and <= JournalEntry.MaxLines : !required)
            .WithMessage($"A journal has 2 to {JournalEntry.MaxLines} lines.");
        validator.RuleForEach(lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.BranchId).NotEmpty();
            line.RuleFor(l => l.Debit).GreaterThanOrEqualTo(0m);
            line.RuleFor(l => l.Credit).GreaterThanOrEqualTo(0m);
            line.RuleFor(l => l.Description).MaximumLength(JournalLine.MaxDescriptionLength);
            line.RuleFor(l => l.PartyType).IsInEnum();
            line.RuleFor(l => l.PartyId).NotEqual(Guid.Empty).When(l => l.PartyId.HasValue);
        });
    }
}
