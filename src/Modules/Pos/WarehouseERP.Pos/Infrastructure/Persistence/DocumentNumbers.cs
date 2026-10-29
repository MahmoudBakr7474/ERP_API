using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Pos.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Pos.Contracts;

namespace WarehouseERP.Pos.Infrastructure.Persistence;

// The series row is locked until the numbering transaction commits: numbers come out one by one per terminal, and a
// rolled-back sale gives its number back with everything else.
internal sealed class DocumentNumbers(DbContext db) : IDocumentNumbers
{
    private const string Next = """
        DECLARE @next int;
        UPDATE [pos].[PosNumberSequences] WITH (UPDLOCK, HOLDLOCK)
        SET @next = [LastNumber] = [LastNumber] + 1
        WHERE [DocumentType] = @type AND [TerminalId] = @terminal AND [FiscalYear] = @year;
        IF @@ROWCOUNT = 0
        BEGIN
            SET @next = 1;
            INSERT INTO [pos].[PosNumberSequences] ([DocumentType], [TerminalId], [FiscalYear], [LastNumber])
            VALUES (@type, @terminal, @year, 1);
        END
        SET @result = @next;
        """;

    public async Task<int> NextAsync(PosDocumentType documentType, Guid terminalId, short fiscalYear, CancellationToken ct)
    {
        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await db.Database.ExecuteSqlRawAsync(Next,
        [
            new SqlParameter("@type", SqlDbType.TinyInt) { Value = (byte)documentType },
            new SqlParameter("@terminal", SqlDbType.UniqueIdentifier) { Value = terminalId },
            new SqlParameter("@year", SqlDbType.SmallInt) { Value = fiscalYear },
            result,
        ], ct);
        return (int)result.Value;
    }
}
