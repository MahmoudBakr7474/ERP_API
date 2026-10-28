using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.Purchasing.Application.Abstractions.Interfaces.Posting;
using WarehouseERP.Purchasing.Contracts;

namespace WarehouseERP.Purchasing.Infrastructure.Persistence;

// The series row is locked until the approving or posting transaction commits: numbers come out one by one, and a
// rolled-back document gives its number back with everything else.
internal sealed class DocumentNumbers(DbContext db) : IDocumentNumbers
{
    private const string Next = """
        DECLARE @next int;
        UPDATE [purchasing].[DocumentNumberSequences] WITH (UPDLOCK, HOLDLOCK)
        SET @next = [LastNumber] = [LastNumber] + 1
        WHERE [DocumentType] = @type AND [BranchId] = @branch AND [FiscalYear] = @year;
        IF @@ROWCOUNT = 0
        BEGIN
            SET @next = 1;
            INSERT INTO [purchasing].[DocumentNumberSequences] ([DocumentType], [BranchId], [FiscalYear], [LastNumber])
            VALUES (@type, @branch, @year, 1);
        END
        SET @result = @next;
        """;

    public async Task<int> NextAsync(PurchasingDocumentType documentType, Guid branchId, short fiscalYear, CancellationToken ct)
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
