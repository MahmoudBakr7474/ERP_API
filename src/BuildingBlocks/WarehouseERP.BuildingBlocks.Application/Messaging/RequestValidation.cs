using FluentValidation;
using WarehouseERP.SharedKernel.Results;

namespace WarehouseERP.BuildingBlocks.Application.Messaging;

// Input validation only (shape, lengths, ranges). Domain invariants stay in the domain and
// database constraints stay authoritative.
internal static class RequestValidation
{
    public static async Task<Error?> ValidateAsync<TRequest>(
        IEnumerable<IValidator<TRequest>> validators, TRequest request, CancellationToken ct)
    {
        List<ValidationError>? errors = null;
        var context = new ValidationContext<TRequest>(request);
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(context, ct);
            foreach (var failure in result.Errors)
                (errors ??= []).Add(new ValidationError(failure.PropertyName, failure.ErrorCode, failure.ErrorMessage));
        }
        return errors is null ? null : Error.Validation(errors);
    }
}

public sealed class ValidatingCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> inner, IEnumerable<IValidator<TCommand>> validators)
    : ICommandHandler<TCommand, TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken ct) =>
        await RequestValidation.ValidateAsync(validators, command, ct) is { } error
            ? error
            : await inner.HandleAsync(command, ct);
}

public sealed class ValidatingQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> inner, IEnumerable<IValidator<TQuery>> validators)
    : IQueryHandler<TQuery, TResponse>
{
    public async Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken ct) =>
        await RequestValidation.ValidateAsync(validators, query, ct) is { } error
            ? error
            : await inner.HandleAsync(query, ct);
}
