using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

// Turns the database failures a module expects into its business errors at the save point.
// Only constraint names a module lists are mapped; anything else is rethrown for the host's
// exception handler, so an unexpected duplicate is never disguised as a known conflict.
public static partial class DbFailures
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    // SQL Server names the index (2601) or constraint (2627) in the message; the message is in the
    // login's language, us_english by default.
    [GeneratedRegex(@"(?:unique index|constraint) '(?<name>[^']+)'", RegexOptions.CultureInvariant)]
    private static partial Regex ConstraintNameRegex();

    /// <summary>The violated unique index or constraint, or null when the failure is something else.</summary>
    public static string? UniqueConstraintName(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: UniqueIndexViolation or UniqueConstraintViolation } sql
        && ConstraintNameRegex().Match(sql.Message) is { Success: true } match
            ? match.Groups["name"].Value
            : null;

    /// <summary>
    /// Saves, mapping a known unique violation to its error and a concurrency conflict to
    /// <see cref="CommonErrors.ConcurrencyConflict"/>. Other failures propagate.
    /// </summary>
    public static async Task<Result> SaveAsync(
        DbContext db, IReadOnlyDictionary<string, Error> knownUniqueConstraints, CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return CommonErrors.ConcurrencyConflict;
        }
        catch (DbUpdateException ex) when (UniqueConstraintName(ex) is { } name
                                           && knownUniqueConstraints.TryGetValue(name, out var error))
        {
            return error;
        }
    }
}
