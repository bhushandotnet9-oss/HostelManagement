# Tasks

## 1. Data model and migration

- [x] 1.1 Add a `PaymentType` enum (`Rent`, `Deposit`) to `Models/Enums.cs`; verify `dotnet build HostelManagement.slnx` succeeds
- [x] 1.2 On `Models/Payment.cs`, add `PaymentType Type`, a required `Guid ResidentId` with a `Resident?` navigation (no default value for `ResidentId` — see design.md Risks), and change `DuePeriodId` to `Guid?` with `DuePeriod?` kept nullable; add a `List<Payment> Payments` navigation to `Models/Resident.cs`; verify the build succeeds
- [x] 1.3 Generate the migration with `dotnet ef migrations add AddPaymentTypeAndDepositPayments --project HostelManagement.Server`; verify the new files appear under `HostelManagement.Server/Data/Migrations`
- [x] 1.4 Edit the generated migration to backfill existing rows — `UPDATE Payments SET ResidentId = (SELECT ResidentId FROM DuePeriods WHERE DuePeriods.Id = Payments.DuePeriodId)` placed *after* SQLite's create-copy-drop-rename rebuild of the table, and confirm `Type` lands on `Rent` (enum 0) and the `DuePeriods` FK survives with its original delete behavior; verify by reading the generated SQL from `dotnet ef migrations script --project HostelManagement.Server`
- [x] 1.5 Verify the backfill against a non-empty database: on a copy of a dev DB that already has payments, run the app so `Database.Migrate()` applies, then confirm every `Payments` row has a non-empty `ResidentId` matching its due period's resident and `Type = 0`

## 2. Rent payments carry their resident

- [x] 2.1 In `DuesController.LogPayment`, set `Type = PaymentType.Rent` and `ResidentId = duePeriod.ResidentId` on the new `Payment`; verify `dotnet test HostelManagement.Server.Tests --filter "FullyQualifiedName~RentBillingTests"` still passes
- [x] 2.2 Add a `RentBillingTests` case asserting a payment logged against a due period is stored with type `Rent` and the due period's `ResidentId`; verify the new test passes

## 3. Deposit payment endpoint

- [x] 3.1 Add `LogDepositPaymentRequest(decimal Amount, DateOnly Date)` and `DepositPaymentResponse(Guid Id, Guid ResidentId, decimal Amount, DateOnly PaidDate)` to `Controllers/Dtos/ResidentDtos.cs`; verify the build succeeds
- [x] 3.2 Add `POST /api/residents/{id}/deposit-payments` to `ResidentsController`: `404` for an unknown or other-org resident, `409` when `DepositAmount` is zero (message naming that no deposit is agreed), `409` when `collected + amount` would exceed `DepositAmount` (message naming the remaining outstanding), otherwise insert a `Deposit` payment with `DuePeriodId = null` and return it; verify with new `RentBillingTests` cases for the happy path, the instalment case, the zero-deposit rejection, and the over-collection rejection leaving the collected total unchanged
- [x] 3.3 Add a `RentBillingTests` case that logging a deposit for a resident with an unpaid and an overdue due period leaves both periods' `AmountPaid`, `IsPaid`, and overdue status untouched; verify it passes
- [x] 3.4 Add a `RentBillingTests` case that a deposit can be logged for a resident on their move-in date before any due period has been materialized; verify it passes
- [x] 3.5 Add `GET /api/residents/{id}/deposit-payments` returning the resident's `Deposit` payments ordered by paid date, `404` for an unknown or other-org resident; verify with tests covering two instalments listed and an empty list for a resident who has paid nothing
- [x] 3.6 Add a `TenancyFilterTests` case that an owner logging a deposit payment for another org's resident gets `404` and no payment row is created; verify it passes

## 4. Deposit collected and outstanding on the resident

- [x] 4.1 Extend `ResidentResponse` with `DepositCollected` and `DepositOutstanding` and update `ResidentsController.ToResponse` to take the collected amount, computing outstanding as `Math.Max(0, DepositAmount - collected)`; verify the build succeeds
- [x] 4.2 In `ResidentsController.List`, compute deposit sums in a single grouped query over `Deposit` payments into a dictionary keyed by `ResidentId` and look up per resident while mapping — not a per-resident query; verify by a `ResidentManagementTests` case listing multiple residents with differing deposit payments and by confirming no query-per-resident in the implementation
- [x] 4.3 Supply the single-resident collected sum in `Get`, `Update`, `AssignCot`, `RecordNotice`, and `Checkout` responses; verify `dotnet test HostelManagement.Server.Tests --filter "FullyQualifiedName~ResidentManagementTests"` passes
- [x] 4.4 Add `ResidentManagementTests` cases for collected/outstanding at zero paid, part paid, fully paid, and after the agreed `DepositAmount` is lowered below what was collected (outstanding floors at 0, collected reports in full); verify they pass
- [x] 4.5 Add a `ResidentManagementTests` case that assigning a cot with an agreed deposit and logging no deposit payment reports the full agreed amount as outstanding; verify it passes

## 5. Resident delete guard

- [x] 5.1 Rewrite the payment guard in `ResidentsController.Delete` from `p.DuePeriod!.ResidentId == id` to `p.ResidentId == id`, keeping the existing comment's intent and noting that a nullable `DuePeriodId` is why the old form no longer suffices; verify the existing delete tests still pass
- [x] 5.2 Add `ResidentManagementTests` cases that a resident with only a deposit payment cannot be deleted (`409` naming recorded payments) and that a resident with no payments of either type still deletes, clearing due periods and returning the cot to `Vacant`; verify they pass

## 6. Angular deposit UI

- [x] 6.1 Add `depositCollected` and `depositOutstanding` to the `Resident` interface and a `logDepositPayment(residentId, amount, date)` plus `listDepositPayments(residentId)` to `resident.service.ts`; verify with `resident.service.spec.ts` cases asserting the request URL, method, and body
- [x] 6.2 In `residents.component.{ts,html}`, show agreed / collected / outstanding together in the resident's deposit area and add a deposit-payment form (amount + date) that calls the service, refreshes the resident list, and raises a success or error toast via `toast.service`; verify with `residents.component.spec.ts` cases for a successful submission and a rejected (`409`) submission showing the error toast
- [ ] 6.3 Label the assignment and edit deposit inputs so the agreed figure reads as agreed rather than collected, consistent with the resident-management delta; verify the existing `e2e/tests/rent-billing.spec.ts` and `resident-assignment.spec.ts` selectors for "Deposit amount" still resolve, updating them in the same commit if a label changes

## 7. Dashboard revenue stays rent-only

- [ ] 7.1 Confirm no existing dashboard query sums payments (the revenue endpoint described in `owner-dashboard` is unimplemented — `RevenueSummaryResponse` exists with no action serving it) and, if any payment-summing query is found, filter it to `PaymentType.Rent`; verify `dotnet test HostelManagement.Server.Tests --filter "FullyQualifiedName~OwnerDashboardTests"` passes
- [ ] 7.2 Add an `OwnerDashboardTests` case that a resident's deposit payment does not appear in any dashboard collected-revenue figure, or — if the revenue endpoint is still absent — record that gap as a plain note in this change so the eventual implementation inherits the rent-only constraint; verify the test passes or the note is present

## 8. Integration verification

- [ ] 8.1 Add an `e2e/tests/rent-billing.spec.ts` case that collects a deposit for an assigned resident through the UI and asserts outstanding drops to zero; verify with `npx playwright test tests/rent-billing.spec.ts` inside `e2e/`
- [ ] 8.2 Run the full suites — `dotnet test HostelManagement.Server.Tests`, `npm test` in `hostelmanagement.client`, and `npm test` in `e2e/` — and verify all pass
- [ ] 8.3 Run `openspec validate add-deposit-payment-type` and verify the change reports valid

## Workflow follow-up

- Archive the change once implementation is reviewed and the suites above are green.
