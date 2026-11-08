# Proposal

## Why

Every record in the system is currently write-once: the API exposes only `GET` and `POST`, so a typo in a hostel name, a wrong rent amount, a mislabelled cot, or an expense entered against the wrong date can never be fixed or removed. Owners are accumulating permanently wrong data with no recourse other than direct database access.

## What Changes

- Add update (`PUT`) and delete (`DELETE`) endpoints for the five entities the owner can currently create: hostels, rooms, cots, residents, and expenses.
- Deletes are **referentially guarded**: a record with dependents is refused with `409 Conflict` and a message naming the blocker, rather than cascading. Specifically:
  - Hostel — blocked by any room, resident, or expense
  - Room — blocked by any cot
  - Cot — blocked while `Occupied` (a resident is assigned)
  - Resident — blocked by any recorded payment; otherwise deleting frees the assigned cot back to `Vacant`
  - Expense — never blocked
- Updates cover descriptive and financial fields only; they do not reassign records across parents or bypass lifecycle rules:
  - Hostel — name, location
  - Room — name
  - Cot — label (status stays driven by assignment/checkout, not editable)
  - Resident — KYC fields (name, contact number, Aadhaar number, move-in date) and the assignment terms (rent amount, deposit amount, billing cycle) when a cot is assigned; `Status`, `CotId`, notice, and checkout fields remain changeable only through the existing lifecycle actions
  - Expense — category, amount, date, notes
- Validation on update mirrors the existing create validation (required text fields non-blank, expense amount positive), returning `400 Bad Request` on failure.
- Changing a resident's rent amount or billing cycle affects **unpaid, not-yet-due** billing periods only; already-paid and already-past-due periods keep their original amounts, so history is never retroactively rewritten.
- A resident's move-in date can only be corrected while no billing period exists yet — it anchors the whole billing schedule, so changing it later would strand already-generated periods. After that, the request is refused with `409 Conflict` and the resident's other fields remain correctable.
- Angular UI gains inline edit and delete affordances for hostels, rooms, cots, residents, and expenses, with a confirmation step before any delete and existing toast notifications for success and for the `409` blocked-delete case. This also requires an expense list view, which the UI does not have today (expenses are currently write-only with a category breakdown).
- All endpoints stay inside the existing multi-tenant model: a record belonging to another owner org is invisible and therefore returns `404 Not Found`, not `403`.
- No **BREAKING** changes — all existing endpoints and response shapes are unchanged.

## Capabilities

### New Capabilities

None. This change adds editing and removal behavior to capabilities that already exist.

### Modified Capabilities

- `hostel-inventory`: adds requirements for renaming hostels, rooms, and cots, and for deleting them with referential guards (occupied cots and non-empty parents cannot be removed).
- `resident-management`: adds requirements for correcting a resident's KYC and assignment terms, for deleting a resident record (blocked once payments exist, frees the cot otherwise), and for how a terms correction interacts with existing billing periods.
- `expense-tracking`: adds requirements for editing and deleting a recorded expense, and for listing a hostel's individual expense entries so they can be selected for edit or delete.

## Impact

- **API** — `HostelsController`, `RoomsController`, `CotsController`, `ResidentsController`, `ExpensesController`: one `PUT` and one `DELETE` each. `ExpensesController` additionally needs a per-expense `GET` route so `PUT`/`DELETE` can address a single expense.
- **DTOs** (`Controllers/Dtos`) — new `Update*Request` records for hostel, room, cot, resident, and expense.
- **Data model / schema** — none. No new entities, columns, or migrations; deletes are physical and guarded in the controller, so no soft-delete flags are introduced.
- **Billing** (`Billing/DuePeriodMaterializer`, `DueDateCalculator`) — unchanged logic, but resident-terms updates must repoint unpaid future `DuePeriod` rows, which touches the same tables the materializer owns.
- **Server tests** (`HostelManagement.Server.Tests`) — new cases in `HostelInventoryTests`, `ResidentManagementTests`, `ExpenseTrackingTests`, plus cross-tenant 404 coverage alongside `TenancyFilterTests`.
- **Frontend** (`hostelmanagement.client`) — `hostel.service.ts`, `resident.service.ts`, `expense.service.ts` gain update/delete/list methods; `hostels.component`, `residents.component`, `expenses.component` gain edit and delete UI; Jest specs updated.
- **E2E** (`e2e/tests`) — new Playwright coverage for the edit and delete flows, including a blocked-delete path.
- **Docs** — `CLAUDE.md`'s API surface description, if it needs to note the expanded verb coverage.
