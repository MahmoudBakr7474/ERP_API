# ADR 0008 — Domain events, integration events, outbox and idempotency (deferred to Inventory)

Status: accepted as design; not implemented (2026-10-26)

## Context
No current use case publishes anything to another module, so a dispatcher, outbox or idempotency store now would be
unused code (and the latter two need new tables, which this refactoring may not add).

## Decision (to implement with the first Inventory posting)
* **Domain events** are raised by aggregates and handled in-process, inside the same transaction, by the same
  module. **Integration events** are the only thing other modules receive; their shapes live in the publisher's
  Contracts.
* An integration event is written to an `integration.OutboxMessages` table in the same transaction as the change
  that caused it, then delivered by a background worker with retries. Consumers record handled event ids in
  `integration.InboxMessages` (unique per consumer) so a redelivery has no second effect.
* Commands with external retry risk (POS sync, postings) carry an idempotency key; the posting stores it with a
  unique constraint in the same transaction, so a repeated request returns the first outcome.
* Creation of master data is already idempotent enough through unique business keys (`SKU`, warehouse code per
  branch → 409).

## Consequences
Inventory's first migration adds the outbox/inbox tables and the explicit transaction port (ADR 0004).
