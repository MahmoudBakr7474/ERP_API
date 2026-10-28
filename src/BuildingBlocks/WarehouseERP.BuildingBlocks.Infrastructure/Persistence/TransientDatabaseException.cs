namespace WarehouseERP.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// A failure the database reports without raising an error, such as being chosen as a deadlock victim while waiting
/// for an application lock. <see cref="EfTransactionRunner"/> runs the work again.
/// </summary>
public sealed class TransientDatabaseException : Exception
{
    public TransientDatabaseException() { }

    public TransientDatabaseException(string message) : base(message) { }

    public TransientDatabaseException(string message, Exception innerException) : base(message, innerException) { }
}
