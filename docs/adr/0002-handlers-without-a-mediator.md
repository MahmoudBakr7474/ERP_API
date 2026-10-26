# ADR 0002 — CQRS with plain handlers and decorators, no mediator

Status: accepted (2026-10-26)

## Context
Use cases need a shared pipeline (input validation, logging) that applies whether a command comes from HTTP or
from another handler. MediatR is the usual answer; since 2025 it is commercially licensed and its runtime dispatch
hides which handler runs.

## Decision
`ICommandHandler<TCommand, TResponse>` and `IQueryHandler<TQuery, TResponse>` (BuildingBlocks.Application) return
`Result<TResponse>`. `AddCommandHandler<,,>` / `AddQueryHandler<,,>` register the feature handler behind
decorators: commands get logging → validation → handler; queries get validation → handler. Callers depend on the
interface, so the pipeline cannot be skipped by accident. Feature folders hold `Command`/`Query`, `Handler`,
`Validator`.

## Alternatives
MediatR (license, reflection-based dispatch, one more dependency); Scrutor for decoration (not needed for two
decorators); validation in endpoints (would not cover internal calls).

## Consequences
One registration line per feature. Adding a cross-cutting behaviour means a new decorator in
`HandlerRegistration`. Navigation from endpoint to handler is a plain "go to implementation".
