using WarehouseERP.Organization.Contracts;
using WarehouseERP.Sales.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Sales.Application.Errors;
using WarehouseERP.Sales.Contracts;
using WarehouseERP.Sales.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Sales.Application.Shared;

// Final numbers per document type, branch and fiscal year (the calendar year in the company's time zone), taken inside
// the transaction that issues, confirms, posts or approves the document: a rolled-back document gives its number back.
public sealed class DocumentNumbering(
    IDocumentNumbers numbers, ICompanyDirectory companies, IBranchDirectory branches, TimeProvider clock)
{
    public async Task<Result<DocumentNumber>> NextAsync(SalesDocumentType documentType, Guid branchId, CancellationToken ct)
    {
        var company = await companies.GetAsync(ct);
        if (company is null)
            return ReferenceErrors.CompanySetupRequired;
        var branch = (await branches.FindManyAsync([branchId], ct)).Single();
        var zone = TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId);
        var fiscalYear = (short)TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime, zone).Year;
        var sequence = await numbers.NextAsync(documentType, branchId, fiscalYear, ct);
        return new DocumentNumber(SalesNumbers.Format(documentType, branch.Code, fiscalYear, sequence), fiscalYear, sequence);
    }
}
