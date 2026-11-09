# Design

## Context

See proposal.md — Why.

The constraints that shape the approach:

- `Payment` currently has a required `DuePeriodId` and reaches a resident only through `DuePeriod.ResidentId`. A deposit has no due period, so that path cannot carry one.
- `Payment` is `ITenantScoped`, so `AppDbContext` stamps `OwnerOrgId` on insert and filters every read by it. Nothing new is needed for tenancy, and a cross-org resident is already invisible to the query filter — hence `404`, per the project's established convention.
- The deposit's declared figure lives on `Resident.DepositAmount`, set at cot assignment and editable through `PUT /api/residents/{id}` (the update path deliberately does not touch already-generated due periods).
- SQLite + EF Core, migrations auto-applied by `Database.Migrate()` at startup and in `CustomWebApplicationFactory.InitializeDatabaseAsync`. Two migrations exist (`InitialCreate`, `AddResidentHostelId`). There is real data in developer/production databases, so the migration must backfill rather than assume an empty table.
- `DuePeriodMaterializer` creates due periods lazily on read. Deposit collection must not depend on it — the deposit is owed from move-in, and a deposit may be taken before anything materializes.

## Goals / Non-Goals

**Goals:**

- One payment table that can represent both rent and deposit payments, with the type explicit rather than inferred from whether a due period is attached.
- A resident-level path from a payment to its resident that works for both types, so deposit sums and the delete guard are single queries rather than joins through `DuePeriod`.
- Deposit collected/outstanding computed from payments on read, never stored as a second mutable balance that can drift from the payments behind it.
- Listing residents stays a bounded number of queries as deposit figures are added.

**Non-Goals:**

- A general ledger or payment-reversal/void feature. Payments remain append-only, as they are today.
- Any change to how rent due periods are generated, materialized, or settled.
- Reconciling checkout's `DepositRefundAmount` against collected deposit (explicitly deferred — see proposal.md).
- Implementing the dashboard revenue endpoint. Only its spec wording changes here.

## Decisions

### Store both payment kinds in one `Payment` table, discriminated by `PaymentType`

`Payment` gains `PaymentType Type` and a required `Guid ResidentId`; `DuePeriodId` becomes `Guid?`. A rent payment has both set; a deposit payment has `DuePeriodId == null`.

*Why:* The two kinds share their entire shape (amount, date, tenancy) and the owner thinks of them as one list of money received. One table keeps the resident-delete guard, any future "all payments for this resident" view, and reporting as single-table queries.

*Alternatives considered:* A separate `DepositPayment` table. It would avoid the nullable FK and make over-collection checks local, but it duplicates the tenancy plumbing, forces every "did this resident pay anything" question into a two-table union (the delete guard, future statements), and makes the "payment type" the user asked for a table name rather than a field — awkward if a third type (late fee, utility) ever appears.

*Rejected:* attaching deposit payments to a due period with a type tag. A deposit may be collected before any due period exists, so there is nothing to attach to, and it would re-introduce exactly the distortion this change removes.

### Denormalize `ResidentId` onto every payment, including rent payments

Rent payments already know their resident transitively through `DuePeriod`. Setting `ResidentId` on them too is redundant, and accepted deliberately.

*Why:* It makes `ResidentId` non-nullable, so every payment has one uniform way to reach its resident. The alternative — resident via `DuePeriod` for rent, via `ResidentId` for deposits — means every query that spans both types needs a `CASE`/union, including the resident-delete guard.

*Trade-off:* `ResidentId` on a rent payment must agree with `DuePeriod.ResidentId`. Nothing but `DuesController.LogPayment` inserts rent payments and it sets both from the same due period, so the two cannot diverge in practice; no DB-level constraint is added to enforce it.

### Derive deposit collected; never store a balance

`DepositCollected` is `SUM(Amount)` over the resident's `Deposit` payments, computed per request. `DepositOutstanding` is `Max(0, DepositAmount - DepositCollected)`.

*Why:* `DuePeriod.AmountPaid` is the existing counter-example — a stored running total that must be kept in step with inserts. For the deposit there is no equivalent of `IsPaid` driving billing behavior, so there is no reason to take on that duplication. Deriving it also means lowering the agreed `DepositAmount` after collection cannot corrupt a stored figure; it just produces an outstanding of zero (specified in the resident-management delta).

*The floor at zero matters:* `PUT /api/residents/{id}` can lower `DepositAmount` below what was collected, so a raw subtraction would report a negative outstanding. Clamping keeps "outstanding" meaning "still to collect".

### Compute deposit sums for the resident list in one grouped query

`ResidentsController.List` returns every resident. Summing deposits per resident inside the projection loop would be one query per resident.

*Why:* A single `GroupBy(p => p.ResidentId)` over `Deposit`-typed payments, materialized into a dictionary and looked up while mapping, keeps `List` at two queries regardless of resident count. `Get`, `Update`, `AssignCot`, `Checkout`, and the notice action each need only the one resident's sum.

### Over-collection check reads the current sum inside the request

`POST /api/residents/{id}/deposit-payments` sums existing deposit payments, rejects with `409` if `collected + amount > DepositAmount`, then inserts.

*Why:* It matches the guarded-write convention already used across the five owner-facing resources (a `409` naming the blocker). A read-then-write race could in principle let two concurrent requests both pass the check; this is a single-owner data-entry app with no concurrent submission path for one resident, and the consequence is an over-collected deposit visible to the owner rather than corruption. Not worth a transaction or a stored balance with a constraint.

Rejecting a deposit payment when `DepositAmount` is zero also covers the unassigned-resident case for free: terms can only be set on a resident with a cot, so an unassigned resident's `DepositAmount` is always zero. The validation is written against `DepositAmount == 0` rather than `CotId == null` so it stays correct if that ever changes, and the message names the agreed deposit as the reason.

### Fix the resident-delete guard as part of the model change, not as a follow-up

`ResidentsController.Delete` guards on `_db.Payments.AnyAsync(p => p.DuePeriod!.ResidentId == id)`. Once `DuePeriodId` is nullable this still compiles and still passes its existing tests, but silently stops seeing deposit payments — a resident with money taken becomes deletable. It is rewritten to `p.ResidentId == id`.

*Why it belongs here:* the defect is introduced by this change's schema edit, so it must land in the same change rather than being discovered later.

### Deposit payments are listable via their own endpoint

`GET /api/residents/{id}/deposit-payments` returns the `Deposit`-typed payments for a resident.

*Why:* The collected total alone cannot explain itself when a deposit arrived in instalments. A dedicated endpoint keeps `ResidentResponse` from growing a nested collection that every resident-list caller would pay for.

## Risks / Trade-offs

- **Migration must backfill, not just add columns** — existing payments are all rent, and `ResidentId` cannot be inferred by EF. → The migration adds the columns, then runs explicit SQL: `Type` to `Rent` (enum value 0, so the column default already lands there) and `UPDATE Payments SET ResidentId = (SELECT ResidentId FROM DuePeriods WHERE DuePeriods.Id = Payments.DuePeriodId)`. Verify against a database that already has payments, not only a fresh one.

- **Making `DuePeriodId` nullable rebuilds the table on SQLite** — SQLite cannot alter a column's nullability in place, so EF generates a create-copy-drop-rename sequence. → Review the generated migration before accepting it: confirm the backfill `UPDATE` runs *after* the rebuild and that the FK to `DuePeriods` survives with the right delete behavior. Do not hand-edit the rebuild; reorder the data step instead.

- **A nullable FK weakens the "rent payment has a due period" invariant at the schema level** — a rent-typed payment with a null `DuePeriodId` is now representable. → Only `DuesController.LogPayment` creates rent payments and it always sets it. Accepted rather than adding a check constraint, consistent with how the project leaves `Resident.CotId`/`HostelId` consistency to the controllers.

- **Reading `DepositAmount` as "already paid" is now wrong** — the resident-management spec previously called it "deposit amount paid", and the assignment form labels it "Deposit amount". Owners may read the agreed figure as collected. → The UI shows agreed, collected, and outstanding together in the resident's deposit section so the distinction is visible where the number is; the resident-management delta records the reinterpretation.

- **No compile-time signal that an insert site forgot `ResidentId`** — every existing test creates payments through `POST /api/dues/{id}/payments` rather than constructing `Payment` directly, so no test breaks to flag a missed field, and an unset `Guid` silently becomes `Guid.Empty`. → `DuesController.LogPayment` is the only rent-payment insert site and is covered by the new assertion that a logged rent payment carries the resident's id; keep `ResidentId` without a default so a future insert site has to think about it.

## Migration Plan

1. Add `PaymentType` and the `Payment` fields; generate the migration with `dotnet ef migrations add AddPaymentTypeAndDepositPayments --project HostelManagement.Server`.
2. Inspect the generated migration, add the `ResidentId` backfill `UPDATE` after the table rebuild, and confirm `Down` is coherent (dropping the new columns and restoring `DuePeriodId` to non-nullable — which will fail if deposit payments exist, an acceptable one-way door for this app).
3. Deploy; `Database.Migrate()` applies it at startup.

Rollback: reverting the code without reverting the database is safe for reads — the extra columns are simply ignored by the old model — so a code-only rollback is the recommended path if a problem appears after deploy. Reverting the migration itself is only viable before any deposit payment is logged.

## Open Questions

- Whether the owner eventually wants checkout to refuse a refund larger than the deposit actually collected. Deferred by the owner for now; adding it later is a new requirement on `resident-management` and does not change anything decided here.
