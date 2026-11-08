# Design

## Context

See `proposal.md` — Why. The constraints that shape the approach:

- **Tenancy is already handled.** `AppDbContext` applies a global query filter per `ITenantScoped` entity, so `_db.Hostels.SingleOrDefaultAsync(h => h.Id == id)` already returns `null` for another org's row. Every new endpoint can look up by id exactly like the existing `GET` handlers and return `NotFound()` — cross-tenant isolation needs no new code, only test coverage proving it holds for the new verbs.
- **No cascade delete is configured.** The model declares only the `Resident`↔`Cot` one-to-one relationship explicitly; everything else uses EF Core conventions. Rather than relying on whatever cascade behavior conventions produce (and on SQLite enforcing it), deletes are guarded explicitly in the controller before `Remove` is called.
- **Dues are materialized lazily and only up to today.** `DuePeriodMaterializer.MaterializeAsync` calls `DueDateCalculator.DueDatesUpTo(moveInDate, cycle, today)`, so **every `DuePeriod` row that exists has `DueDate <= today`**. There is no such thing as a stored future-dated period. This makes the "corrected terms apply only to future unpaid dues" requirement almost free to satisfy (see Decisions), but it also means the move-in date is a live schedule anchor, not just a display field.
- **The expense UI is write-only today.** `ExpenseService` has `create` and `breakdown` but no list, and the server has no per-expense route. Edit/delete UI for expenses is impossible without adding both.
- **Controllers are thin and uniform.** Each action validates inline, touches `_db` directly, and maps to a DTO record via a local projection or a private `ToResponse`. No service or repository layer exists, and this change should not introduce one.

## Goals / Non-Goals

**Goals:**

- One `PUT` and one `DELETE` per entity, following the shape and naming of the existing actions so the controllers stay uniform and readable.
- A single, predictable status-code contract across all ten new endpoints (below), so the Angular layer can handle errors generically.
- Guard conditions expressed as explicit existence checks with messages an owner can act on ("This hostel still has rooms"), not bare status codes.
- Billing history is immutable: no correction or deletion path may alter a `DuePeriod` that has been paid or has come due.

**Non-Goals:**

- No service/repository layer, no `AutoMapper`, no generic CRUD base controller. Ten short actions in five existing controllers is less code than any abstraction over them.
- No `PATCH` / partial update. `PUT` with a full replacement body matches the existing create DTOs and avoids null-means-unchanged ambiguity.
- No optimistic concurrency (`RowVersion` / `If-Match`). Single-owner-per-org usage makes lost-update races a theoretical concern; adding row versions would mean a migration on every table.
- No audit trail of who changed or deleted what. Worth doing, but it is its own change with its own schema.
- No soft delete. Explicitly rejected below.
- No bulk delete, and no "delete this hostel and everything in it" convenience endpoint.

## Decisions

### Guarded hard delete over cascade or soft delete

**Decision:** Physically delete the row, but only after checking for dependents; refuse with `409 Conflict` and an explanatory message otherwise.

**Why:** The data being deleted is almost always a data-entry mistake the owner wants gone, not a historical record worth retaining. Cascade was rejected because deleting a hostel would silently take rooms, cots, residents, and expenses with it — an irreversible mistake one misclick away, in an app with no undo and no audit log. Soft delete was rejected because it needs an `IsDeleted` column on five entities (a migration), a second global query filter that has to compose correctly with the existing tenant filter, and a decision about what "deleted" means for every aggregate query (`occupancy`, `revenue`, `breakdown`, dashboard) — a large blast radius for a feature whose purpose is "let me fix my typo."

**Consequence:** Deleting a populated hostel is a multi-step chore (cots, then rooms, then the hostel). Accepted: that friction is the safety property, and the `409` messages tell the owner exactly what to clear next.

### Resident delete removes dues but is blocked by payments

**Decision:** A resident with zero `Payment` rows can be deleted; their `DuePeriod` rows go with them, and their cot is returned to `Vacant` in the same transaction. A resident with any payment cannot be deleted.

**Why:** Payments are money that actually changed hands — the one thing in this system that must never vanish. Due periods, by contrast, are derived data the materializer regenerates from the resident's terms, so they carry no independent information once the resident is gone. Blocking on `DuePeriod` existence instead would make delete useless: the first `GET /api/residents/{id}/dues` materializes periods for any assigned resident, so a resident would become undeletable just by being looked at.

**Implementation note:** Deleting `DuePeriod` rows explicitly rather than trusting cascade keeps the behavior identical regardless of EF conventions or SQLite foreign-key enforcement, and keeps it visible in the controller next to the guard that permits it.

### Terms corrections rely on the materializer instead of rewriting dues

**Decision:** On a terms correction, update the `Resident` row only. Do not touch existing `DuePeriod` rows.

**Why:** This already satisfies the spec exactly, because of the lazy materializer: every stored period is already due, so "leave stored periods alone" *is* "paid and past-due periods keep their original amounts," and the next materialization reads the new `RentAmount` straight off the resident, so "future periods use the new terms" happens with no extra code. Writing a repointing loop over unpaid future periods would be dead code against a set that is always empty, and it would be a live hazard if the materializer ever starts generating ahead of today — it would then silently rewrite amounts the spec says to preserve.

**Follow-on:** This is worth a code comment at the update site, because the correctness argument lives in the materializer, not in the update action.

### Move-in date is frozen once any due period exists

**Decision:** Reject a move-in date change with `409 Conflict` if the resident has at least one `DuePeriod`; allow the rest of the correction to proceed in that same request only if the submitted move-in date is unchanged.

**Why:** `DueDatesUpTo` derives the entire billing schedule from the move-in date. Moving it earlier makes the materializer mint extra back-dated periods at the *current* rent; moving it later leaves already-generated periods anchored to dates the schedule no longer produces — orphaned rows that no recalculation will ever reconcile. Recomputing the schedule properly means deciding how to reconcile existing payments against shifted periods, which is a billing-correction feature, not a typo fix.

**Alternative considered:** Allow the change and re-derive everything. Rejected as out of proportion to this change; the `409` keeps the system honest, and a resident whose move-in date is genuinely wrong *and* who has dues can be deleted and re-created while no payment exists.

**Detection:** Compare the submitted `MoveInDate` to the stored one and only then check for dues, so a correction that leaves the date alone never trips the guard.

### Status-code contract

Uniform across all new endpoints, so the Angular error handling can be written once:

| Situation | Status |
| --- | --- |
| Update succeeded | `200 OK` with the updated resource |
| Delete succeeded | `204 No Content` |
| Validation failure (blank required field, non-positive amount) | `400 Bad Request` with a message |
| Record absent, or owned by another org | `404 Not Found` |
| Guard violated (dependents exist, cot occupied, payments exist, move-in date frozen) | `409 Conflict` with a message |

`404` for another org's record is deliberate and matches the existing `GET` behavior — the tenant query filter means the controller genuinely cannot see the row, and leaking existence via `403` would be worse.

### Route shapes

`PUT`/`DELETE` sit on the id route of each existing collection, so nothing new needs to be learned:

- `/api/hostels/{id}`
- `/api/hostels/{hostelId}/rooms/{id}`
- `/api/rooms/{roomId}/cots/{id}`
- `/api/residents/{id}`
- `/api/hostels/{hostelId}/expenses/{id}`

Nested routes validate the parent and the child-belongs-to-parent relationship, matching how the existing nested `List`/`Create` actions check the parent first. `RoomsController` and `CotsController` have no single-item `GET` today; adding `PUT`/`DELETE` does not require one, and the UI already has the row data from its list call, so none is added. `ExpensesController` is the exception — it gets `GET /{id}` and a plain `GET` list, because the expense UI has no listing to edit from (see Context).

### Angular: inline edit state, no new routes or modals

**Decision:** Each component tracks an "editing" id in a signal and swaps the row for an inline form; delete goes through the browser's native `confirm()` before firing. Toasts report both success and failure, with the server's `409`/`400` message surfaced rather than a generic string.

**Why:** The three feature components are already signal-based with inline create forms (`newHostelName`, `newCotLabelByRoom`, …), so inline edit is the shape that already fits. A modal component or a routed edit page would be the first of its kind in this codebase and would pull in dependencies (a dialog library, or hand-rolled focus management) for five near-identical forms. `confirm()` is crude but honest about being a blocking safety check, and it keeps the "confirmation step before delete" requirement from becoming a UI sub-project.

**Consequence:** `confirm()` is not easily styleable and is awkward to assert on in Playwright. The e2e tests will need `page.on('dialog', …)` to accept it — noted in tasks so it is not discovered late.

## Risks / Trade-offs

- **Guard-then-delete has a race window.** Two concurrent requests could both pass a guard and then act on now-stale state (e.g. a cot assigned between the vacancy check and the delete). → Each action runs a single `SaveChangesAsync`, and the underlying FK constraints fail the write rather than corrupting data. With one owner per org this is vanishingly unlikely; proper handling means concurrency tokens, which is a Non-Goal.
- **A delete still succeeds where a guard is missing.** The guards are hand-written per entity, so a future entity added without one deletes freely. → The guard list is enumerated in the spec deltas with a scenario each, so the test suite fails if one is dropped; adding a new relationship means revisiting it.
- **Expense list has no paging.** A new unbounded `GET` on expenses will grow with usage. → Matches every other list endpoint in the app; the breakdown endpoint already date-filters, and paging across the API is a separate concern.
- **Correcting rent silently changes future billing.** An owner fixing a typo may not realize the next generated period uses the new amount. → That is the intended behavior, and past periods visibly keep their old amounts; the UI should word the edit form as changing terms going forward.
- **`confirm()` blocks and looks unstyled.** → Accepted for now; the dialog is the cheapest correct implementation of "ask before destroying," and replacing it later touches only the delete handlers.

## Migration Plan

No schema change, no migration, no data backfill — the change is purely additive at the API and UI level. Existing endpoints and response shapes are untouched, so the server and SPA can ship together or independently in either order: a new SPA against an old server would get `404`s on the new routes, and an old SPA against a new server simply never calls them. Rollback is a revert.
