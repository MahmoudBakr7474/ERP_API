using System.Linq.Expressions;
using FluentValidation;
using WarehouseERP.Accounting.Application.Contracts.Vouchers;
using WarehouseERP.Accounting.Domain;

namespace WarehouseERP.Accounting.Application.Features.Vouchers;

// The input shape of a voucher's counter lines and allocations, shared by creating and changing one; what each voucher
// type requires is the domain's business.
internal static class VoucherRequestRules
{
    public static void AddVoucherRules<T>(
        this AbstractValidator<T> validator, Expression<Func<T, string>> description, Expression<Func<T, string?>> reference,
        Expression<Func<T, IEnumerable<VoucherLineRequest>?>> lines, Expression<Func<T, IEnumerable<VoucherAllocationRequest>?>> allocations)
    {
        validator.RuleFor(description).NotEmpty().MaximumLength(JournalEntry.MaxDescriptionLength);
        validator.RuleFor(reference).MaximumLength(JournalEntry.MaxReferenceLength);
        validator.RuleFor(lines).Must(l => l is null || l.Count() <= VoucherLines.MaxLines)
            .WithMessage($"A voucher has at most {VoucherLines.MaxLines} counter lines.");
        validator.RuleForEach(lines).ChildRules(line =>
        {
            line.RuleFor(l => l.AccountId).NotEmpty();
            line.RuleFor(l => l.BranchId).NotEmpty();
            line.RuleFor(l => l.Amount).GreaterThan(0m);
            line.RuleFor(l => l.Description).MaximumLength(JournalLine.MaxDescriptionLength);
        });
        validator.RuleForEach(allocations).ChildRules(allocation =>
        {
            allocation.RuleFor(a => a.OpenItemId).NotEmpty();
            allocation.RuleFor(a => a.Amount).GreaterThan(0m);
        });
    }
}
