# Tasks

Vertical slices, one per capability area: server endpoint → server tests → Angular service/UI → frontend tests. Each group lands its own tests so a later slice never cascades failures back through earlier ones. Group 1 is shared groundwork; group 6 is integration-only.

## 1. Shared groundwork

- [x] 1.1 Add `UpdateHostelRequest(string Name, string Location)` to `Controllers/Dtos/HostelDtos.cs`, `UpdateRoomRequest(string Name)` to `RoomDtos.cs`, `UpdateCotRequest(string Label)` to `CotDtos.cs`, `UpdateExpenseRequest(ExpenseCategory Category, decimal Amount, DateOnly Date, string? Notes)` to `ExpenseDtos.cs`, and `UpdateResidentRequest(string Name, string ContactNumber, string AadhaarNumber, DateOnly MoveInDate, decimal RentAmount, decimal DepositAmount, BillingCycle BillingCycle)` to `ResidentDtos.cs`; verify `dotnet build HostelManagement.slnx` succeeds.
- [x] 1.2 Confirm the status-code contract in `design.md` — Decisions is reachable with the current base class: check that `ControllerBase.NoContent()` and `Conflict(object)` are used consistently with the existing `Conflict("This cot is already occupied.")` call in `ResidentsController.AssignCot`, and note in the PR/commit that `404` (not `403`) is intentional for another org's record.

## 2. Hostel update and delete

- [x] 2.1 Add `PUT /api/hostels/{id:guid}` to `HostelsController`: look up by id (tenant filter makes a foreign row `null` → `NotFound()`), reject blank name or location with `BadRequest`, save, return `200` with `HostelResponse`; verify by hitting the endpoint via a new integration test.
- [x] 2.2 Add `DELETE /api/hostels/{id:guid}` to `HostelsController`: `NotFound()` when absent; `Conflict` with a message naming the blocker when any `Room`, `Resident`, or `Expense` references the hostel; otherwise remove and return `204`.
- [x] 2.3 Add `HostelInventoryTests` cases for hostel update (success, blank-name `400`, cross-org `404` using `MutableOwnerContext`) and hostel delete (empty hostel `204` and gone from the list, hostel-with-rooms `409` with both still present, hostel-with-expenses `409`); verify `dotnet test HostelManagement.Server.Tests --filter "FullyQualifiedName~HostelInventoryTests"` passes.

## 3. Room and cot update and delete

- [x] 3.1 Add `PUT /api/hostels/{hostelId:guid}/rooms/{id:guid}` and `DELETE .../rooms/{id:guid}` to `RoomsController`: validate the parent hostel exists and the room belongs to it (else `NotFound()`), reject blank name with `BadRequest`, refuse delete with `Conflict` while the room has any cot, return `200`/`204` respectively.
- [x] 3.2 Add `PUT /api/rooms/{roomId:guid}/cots/{id:guid}` and `DELETE .../cots/{id:guid}` to `CotsController`: same parent/child checks, reject blank label with `BadRequest`, leave `Status` untouched on update, refuse delete with `Conflict` when `Status == CotStatus.Occupied`, return `200`/`204`.
- [x] 3.3 Add `HostelInventoryTests` cases: room rename success and blank-name `400`; empty-room delete `204`; room-with-cots delete `409`; cot relabel preserving `Occupied` status; vacant-cot delete `204` with the hostel's occupancy total dropping by one; occupied-cot delete `409` with the assignment intact; a room/cot addressed through the wrong parent id returning `404`. Verify the filtered `dotnet test` run passes.

## 4. Resident correction and delete

- [x] 4.1 Add `PUT /api/residents/{id:guid}` to `ResidentsController`: `NotFound()` when absent, `BadRequest` on blank name/contact/Aadhaar, `BadRequest` when rent/deposit/billing-cycle terms are submitted for a resident with no `CotId`; apply KYC and terms to the `Resident` row only and return `200` with `ToResponse`. Leave `Status`, `CotId`, `NoticeDate`, `NoticePeriodDays`, `CheckoutDate`, and `DepositRefundAmount` untouched.
- [x] 4.2 In the same action, guard the move-in date: if the submitted `MoveInDate` differs from the stored one **and** the resident has any `DuePeriod`, return `Conflict` with a "billing has already started" message and save nothing. Add a comment at the update site explaining why no `DuePeriod` row is rewritten (every stored period already has `DueDate <= today`, so the materializer applies new terms to subsequent periods on its own) — see `design.md` — Decisions.
- [x] 4.3 Add `DELETE /api/residents/{id:guid}` to `ResidentsController`: `NotFound()` when absent; `Conflict` when any `Payment` exists for any of the resident's due periods; otherwise delete the resident's `DuePeriod` rows, set the assigned cot (if any) back to `CotStatus.Vacant`, remove the resident, and return `204` in a single `SaveChangesAsync`.
- [x] 4.4 Add `ResidentManagementTests` cases: KYC correction success; blank-Aadhaar `400`; cross-org `404`; terms correction on an assigned resident; terms on an unassigned resident `400`; correction leaving a serving-notice resident's lifecycle fields unchanged; move-in date change allowed with no dues and `409` once dues exist; contact-number correction still allowed after dues exist; delete with no payments `204` (dues gone, cot `Vacant`); delete with a payment `409` (resident, dues, and assignment all intact). Verify `dotnet test HostelManagement.Server.Tests --filter "FullyQualifiedName~ResidentManagementTests"` passes.
- [x] 4.5 Add a case asserting that after a rent correction, a newly materialized due period uses the new amount while the pre-existing due period keeps its original amount (drive it through `GET /api/residents/{id}/dues`); verify it passes.

## 5. Expense list, update, and delete

- [x] 5.1 Add `GET /api/hostels/{hostelId:guid}/expenses/{id:guid}` to `ExpensesController` returning a single `ExpenseResponse` (or `NotFound()`), so `PUT`/`DELETE` have a `CreatedAtAction`-style sibling and the UI can re-read one entry. Keep the existing `breakdown` route resolving correctly alongside the new `{id:guid}` route — the `:guid` constraint is what keeps `breakdown` from being captured as an id.
- [x] 5.2 Add `PUT /api/hostels/{hostelId:guid}/expenses/{id:guid}`: validate the parent hostel and that the expense belongs to it (else `NotFound()`), reject `Amount <= 0` with `BadRequest`, save category/amount/date/notes, return `200`.
- [x] 5.3 Add `DELETE /api/hostels/{hostelId:guid}/expenses/{id:guid}`: `NotFound()` when absent or foreign, otherwise remove and return `204` with no guard.
- [x] 5.4 Add `ExpenseTrackingTests` cases: list entries for a hostel; get one entry; update amount and see the breakdown change; recategorize and see the amount move between categories; non-positive amount `400` with the expense unchanged; cross-org update and delete both `404`; delete `204` with the entry gone from both the list and the breakdown. Verify `dotnet test HostelManagement.Server.Tests --filter "FullyQualifiedName~ExpenseTrackingTests"` passes.

## 6. Angular services

- [x] 6.1 Extend `hostels/hostel.service.ts` with `updateHostel`, `deleteHostel`, `updateRoom`, `deleteRoom`, `updateCot`, `deleteCot` against the routes from group 2–3, typed like the existing methods (`Observable<Hostel>` / `Observable<void>`).
- [x] 6.2 Extend `residents/resident.service.ts` with `update(residentId, …)` and `remove(residentId)`, and `expenses/expense.service.ts` with `list(hostelId)`, `update(hostelId, expenseId, …)`, and `remove(hostelId, expenseId)`.
- [x] 6.3 Add a small shared helper for turning an `HttpErrorResponse` from these calls into the toast message (prefer the server's `400`/`409` body text, fall back to a generic message); verify with a Jest spec covering a `409` with a body, a `400` with a body, and a bodyless failure.
- [x] 6.4 Add Jest specs asserting each new service method issues the expected verb and URL via `HttpTestingController`; verify `npx jest src/app/hostels src/app/residents src/app/expenses` passes.

## 7. Angular hostel, room, and cot UI

- [x] 7.1 In `hostels.component.ts`/`.html`, add an editing-id signal plus inline edit forms for hostel, room, and cot rows (name/location, name, label), saving through the group 6 service methods, reloading the affected list, and raising a success toast — matching the existing `createHostel`/`createRoom`/`createCot` handler shape.
- [x] 7.2 Add delete buttons for hostel, room, and cot rows that call `confirm()` first, then the service, reloading the list and occupancy on success and surfacing the server's `409` message via `toast.error` on failure.
- [x] 7.3 Add a Jest spec for `HostelsComponent` covering: entering and cancelling edit mode; a successful rename reloading the list; a declined `confirm()` issuing no request; a `409` delete showing the server message as an error toast. Verify `npx jest src/app/hostels` passes.

## 8. Angular resident and expense UI

- [x] 8.1 In `residents.component.ts`/`.html`, add an inline edit form for a resident's KYC fields plus (when a cot is assigned) rent, deposit, and billing cycle; word the terms section as applying to future billing. Surface the move-in-date `409` as an error toast rather than a silent failure.
- [x] 8.2 Add a resident delete button behind `confirm()`, surfacing the payment-history `409` message on failure and reloading the resident list on success.
- [x] 8.3 In `expenses.component.ts`/`.html`, add an expense entries list for the selected hostel (using the new `list` method) with inline edit and `confirm()`-guarded delete per row, refreshing both the list and the breakdown after either succeeds.
- [x] 8.4 Add Jest specs for `ResidentsComponent` (edit success, terms shown only when assigned, move-in-date `409` toast, delete `409` toast) and `ExpensesComponent` (entries render, edit refreshes list and breakdown, delete removes the row). Verify `npx jest src/app/residents src/app/expenses` passes.

## 9. End-to-end and documentation

- [x] 9.1 Add Playwright coverage in `e2e/tests/hostel-inventory.spec.ts` for renaming a hostel and deleting a vacant cot, and for the blocked delete of a hostel that still has rooms; use `page.on('dialog', d => d.accept())` for the `confirm()` step (see `design.md` — Decisions) and assert the error toast text for the blocked case.
- [x] 9.2 Add Playwright coverage for correcting a resident and for editing then deleting an expense entry, extending `resident-assignment.spec.ts` and `expense-tracking.spec.ts`; verify `npm test` in `e2e/` passes (its `pretest` rebuilds the SPA into `wwwroot`).
- [x] 9.3 Update the "API surface" section of `CLAUDE.md` to note that the five owner-facing controllers now expose update and delete alongside read and create, and that deletes are guarded rather than cascading; verify the described verbs match the implemented routes.

## 10. Full verification

- [x] 10.1 Run `dotnet build HostelManagement.slnx`, the full `dotnet test HostelManagement.Server.Tests`, `npm test` in `hostelmanagement.client`, and `npm test` in `e2e/`; verify all four succeed with no pre-existing test regressed.
- [x] 10.2 Run `openspec validate --change add-update-delete-endpoints` and confirm the three delta specs' scenarios all map to a test or e2e case that now exists.

## Workflow follow-up

- Archive the change with `/opsx:archive` once the implementation is reviewed and the delta specs have been synced into `openspec/specs/`.
