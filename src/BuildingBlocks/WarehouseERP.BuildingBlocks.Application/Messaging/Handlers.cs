using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.BuildingBlocks.Application.Messaging;

// CQRS without a mediator: a command changes state, a query only reads. Endpoints and other
// handlers depend on these interfaces; the registered implementation is the feature handler wrapped
// in the logging and validation decorators, so every caller goes through the same pipeline.

public interface ICommandHandler<in TCommand, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken ct);
}
