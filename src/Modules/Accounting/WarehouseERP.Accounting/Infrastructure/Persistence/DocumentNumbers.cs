using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Accounting.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Accounting.Contracts;

namespace WarehouseERP.Accounting.Infrastructure.Persistence;

// The series row is locked until the posting transaction commits: numbers come out one by one per journal type and fiscal
// year, and a rolled-back posting gives its number back with everything else.
internal sealed class DocumentNumbers(DbContext db) : IDocumentNumbers
{
    private const string Next = """
        DECLARE @next int;
        UPDATE [accounting].[DocumentNumberSequences] WITH (UPDLOCK, HOLDLOCK)
        SET @next = [LastNumber] = [LastNumber] + 1
        WHERE [JournalType] = @type AND [FiscalYearId] = @year;
        IF @@ROWCOUNT = 0
        BEGIN
            SET @next = 1;
            INSERT INTO [accounting].[DocumentNumberSequences] ([JournalType], [FiscalYearId], [LastNumber])
            VALUES (@type, @year, 1);
        END
        SET @result = @next;
        """;

    public async Task<int> NextAsync(JournalType journalType, Guid fiscalYearId, CancellationToken ct)
    {
        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await db.Database.ExecuteSqlRawAsync(Next,
        [
            new SqlParameter("@type", SqlDbType.TinyInt) { Value = (byte)journalType },
            new SqlParameter("@year", SqlDbType.UniqueIdentifier) { Value = fiscalYearId },
            result,
        ], ct);
        return (int)result.Value;
    }
}
