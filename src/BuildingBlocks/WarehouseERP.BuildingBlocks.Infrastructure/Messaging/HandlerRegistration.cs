using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WarehouseERP.BuildingBlocks.Application.Messaging;

namespace WarehouseERP.BuildingBlocks.Infrastructure.Messaging;

// Registers a feature handler behind its pipeline. Whoever resolves ICommandHandler/IQueryHandler,
// an endpoint or another handler, gets validation (and logging for commands) for free.
public static class HandlerRegistration
{
    public static IServiceCollection AddCommandHandler<TCommand, TResponse, THandler>(this IServiceCollection services)
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<ICommandHandler<TCommand, TResponse>>(sp =>
            new LoggingCommandHandler<TCommand, TResponse>(
                new ValidatingCommandHandler<TCommand, TResponse>(
                    sp.GetRequiredService<THandler>(), sp.GetServices<IValidator<TCommand>>()),
                sp.GetRequiredService<ILogger<LoggingCommandHandler<TCommand, TResponse>>>()));
        return services;
    }

    public static IServiceCollection AddQueryHandler<TQuery, TResponse, THandler>(this IServiceCollection services)
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IQueryHandler<TQuery, TResponse>>(sp =>
            new ValidatingQueryHandler<TQuery, TResponse>(
                sp.GetRequiredService<THandler>(), sp.GetServices<IValidator<TQuery>>()));
        return services;
    }
}
