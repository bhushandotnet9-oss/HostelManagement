# Proposal

## Why

Every payment the owner logs today is implicitly a rent payment: `Payment` hangs off a `DuePeriod` and its amount is added straight into that period's `AmountPaid`. The security deposit has no payment record at all — `Resident.DepositAmount` is a single declared figure captured at cot assignment, so the owner cannot record that the deposit was actually collected, cannot collect it in instalments, and has no way to see who still owes it. Owners who take a deposit on move-in day (often separately from the first month's rent) currently either leave it untracked or distort a rent due period by logging it there.

## What Changes

- Payments gain an explicit type: `Rent` or `Deposit`. Every existing payment is a `Rent` payment, so no logged history changes meaning.
- A deposit payment is logged against the **resident**, not against a due period — a new `POST /api/residents/{id}/deposit-payments` endpoint. The deposit is owed from move-in, not on any billing schedule, so it has no due period to attach to and may be collected before the first one exists.
- A deposit payment never touches a due period's `AmountPaid` or `IsPaid`. Logging one cannot settle, part-settle, or un-overdue any rent period.
- `Resident.DepositAmount` is reframed as the **agreed** deposit; what was actually taken is the sum of that resident's `Deposit` payments. The resident response gains `DepositCollected` and `DepositOutstanding` (agreed minus collected, floored at zero) so the owner can see an uncollected or part-collected deposit.
- A deposit payment that would push collected past the agreed `DepositAmount` is refused with `409 Conflict`, matching the project's guarded-write convention. A resident with no agreed deposit (no cot assigned, or `DepositAmount` of zero) cannot take a deposit payment.
- Deposit payments are listable per resident (`GET /api/residents/{id}/deposit-payments`) so the owner can see the instalments behind the collected figure.
- Dashboard revenue stays **rent-only**: deposits are not revenue and must not inflate collected totals. The owner-dashboard spec text is tightened to say "rent payments" where it currently says "logged payments", which with payment types would otherwise read as including deposits.
- The resident's deposit section in the Angular UI gains a deposit-payment form and a collected/outstanding readout alongside the existing agreed amount.
- Deleting a resident continues to be blocked by any recorded payment — now including deposit payments. This needs a code fix, not just new behavior: the existing guard queries `p.DuePeriod!.ResidentId`, which silently stops matching once `DuePeriodId` is nullable.

Deliberately **not** in scope:
- Checkout's `DepositRefundAmount` stays as it is — unvalidated against what was collected. Reconciling the two is a separate decision the owner declined for now.
- The owner-dashboard revenue endpoint itself. The `rent-billing`/`owner-dashboard` specs describe expected-vs-collected revenue and `RevenueSummaryResponse` exists, but no `DashboardController` action serves it. This change only clarifies the requirement's wording so the eventual implementation counts rent only; it does not implement the endpoint.

## Capabilities

### New Capabilities

None. Deposit collection is payment tracking against an existing resident record, so it belongs to the capabilities that already own payments and resident terms rather than to a new one.

### Modified Capabilities

- `rent-billing`: Payment logging gains a payment type. The existing "Manual payment logging" requirement is scoped to rent payments explicitly, and a new requirement covers logging and tracking deposit payments at the resident level, including that they never settle a rent due period.
- `resident-management`: The "Cot assignment" requirement currently records a "deposit amount paid"; it is clarified to record the *agreed* deposit, with the collected amount derived from deposit payments and exposed as collected/outstanding.
- `owner-dashboard`: The "Revenue collected vs expected" requirement is clarified so collected revenue counts rent payments only, now that payments can also be deposits.

## Impact

Backend (`HostelManagement.Server`):
- `Models/Enums.cs` — new `PaymentType` enum (`Rent`, `Deposit`).
- `Models/Payment.cs` — new `Type` and `ResidentId`; `DuePeriodId` becomes nullable.
- `Models/Resident.cs` — new `Payments` navigation.
- `Data/Migrations` — a new EF Core migration (the project has `InitialCreate` and `AddResidentHostelId`; migrations auto-apply via `Database.Migrate()` on startup). Existing rows need backfilling: `Type = Rent` and `ResidentId` resolved through the owning due period.
- `Controllers/ResidentsController.cs` — two new deposit-payment actions; the `Delete` payment guard rewritten to use `Payment.ResidentId`.
- `Controllers/DuesController.cs` — `LogPayment` stamps `Type = Rent` and `ResidentId`.
- `Controllers/Dtos/ResidentDtos.cs` — `ResidentResponse` gains `DepositCollected`/`DepositOutstanding`; new deposit-payment request/response records.

Frontend (`hostelmanagement.client`):
- `residents/resident.service.ts` + `Resident` interface, a deposit-payment service method, and the deposit UI in `residents.component.{ts,html}`.

Tests:
- `HostelManagement.Server.Tests` — `RentBillingTests` (deposit logging, over-collection `409`, rent periods untouched), `ResidentManagementTests` (collected/outstanding, delete guard), `TenancyFilterTests` (cross-org deposit payment is `404`).
- `e2e/tests/rent-billing.spec.ts` — collecting a deposit through the UI.

API compatibility: `ResidentResponse` is additive. `POST /api/dues/{id}/payments` keeps its existing shape and rent-only meaning. No breaking changes.
