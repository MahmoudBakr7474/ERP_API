using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Inventory.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Inventory.Contracts;

namespace WarehouseERP.Inventory.Infrastructure.Persistence;

// The series row is locked until the posting commits: numbers come out one by one, and a rolled-back posting gives its
// number back with everything else.
internal sealed class DocumentNumbers(DbContext db) : IDocumentNumbers
{
    private const string Next = """
        DECLARE @next int;
        UPDATE [inventory].[DocumentNumberSequences] WITH (UPDLOCK, HOLDLOCK)
        SET @next = [LastNumber] = [LastNumber] + 1
        WHERE [DocumentType] = @type AND [BranchId] = @branch AND [FiscalYear] = @year;
        IF @@ROWCOUNT = 0
        BEGIN
            SET @next = 1;
            INSERT INTO [inventory].[DocumentNumberSequences] ([DocumentType], [BranchId], [FiscalYear], [LastNumber])
            VALUES (@type, @branch, @year, 1);
        END
        SET @result = @next;
        """;

    public async Task<int> NextAsync(StockDocumentType documentType, Guid branchId, short fiscalYear, CancellationToken ct)
    {
        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await db.Database.ExecuteSqlRawAsync(Next,
        [
            new SqlParameter("@type", SqlDbType.TinyInt) { Value = (byte)documentType },
            new SqlParameter("@branch", SqlDbType.UniqueIdentifier) { Value = branchId },
            new SqlParameter("@year", SqlDbType.SmallInt) { Value = fiscalYear },
            result,
        ], ct);
        return (int)result.Value;
    }
}
