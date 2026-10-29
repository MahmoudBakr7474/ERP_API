using WarehouseERP.Pos.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Pos.Contracts;
using WarehouseERP.Pos.Domain;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.Pos.Application.Shared;

// Final numbers per terminal, document type and fiscal year (the calendar year in the company's time zone), taken inside
// the transaction that opens the shift or records the sale or return: a rolled-back one gives its number back.
public sealed class DocumentNumbering(IDocumentNumbers numbers, PosReferences references)
{
    public async Task<Result<DocumentNumber>> NextAsync(PosDocumentType documentType, PosTerminal terminal, CancellationToken ct)
    {
        var company = await references.CompanyAsync(ct);
        if (company.IsFailure)
            return company.Error;
        var fiscalYear = (short)references.Today(company.Value).Year;
        var sequence = await numbers.NextAsync(documentType, terminal.Id, fiscalYear, ct);
        return new DocumentNumber(PosNumbers.Format(documentType, terminal.Code, fiscalYear, sequence), fiscalYear, sequence);
    }
}
