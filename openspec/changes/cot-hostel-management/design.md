# Design

## Context

The repo currently contains a default .NET 10 Web API (`HostelManagement.Server`) and Angular SPA (`hostelmanagement.client`) scaffold with no domain code (only the template `WeatherForecastController`). See `proposal.md` - Why for motivation. This design covers how to implement the six capabilities in `proposal.md` on top of that scaffold.

## Goals / Non-Goals

**Goals:**
- Define the data model and tenancy enforcement approach shared by all six capabilities.
- Define the due-date generation algorithm precisely (month-count advance, day-of-month preserved, no proration).
- Define how occupancy status, payments, and checkout interact so cot state stays consistent.

**Non-Goals:**
- Payment gateway integration, SMS/WhatsApp notifications, or any external messaging (explicitly out of scope per proposal).
- Caretaker/staff/resident login or role-based permissions beyond a single owner per org.
- Multi-currency or multi-country configurability (India/₹/Aadhaar is hardcoded for this change).

## Decisions

### Tenancy: single shared schema, Owner Org foreign key
Every domain table (Hostel, Room, Cot, Resident, Payment, Expense) carries an `OwnerOrgId` foreign key. All queries filter by the authenticated owner's `OwnerOrgId`; this is enforced centrally (e.g. a single query filter / interceptor at the data-access layer) rather than repeated in every endpoint, so a missing filter fails closed rather than open.
- **Alternative considered**: separate database per owner. Rejected for v1 - adds operational complexity (migrations x N databases) with no benefit while there is one real tenant (the owner) and a handful of future ones.

### Due-date generation: anchor day-of-month + cycle months
Store each resident's billing cycle as a month count (1/3/6/12). Compute the next due date as `AddMonths(previousDueDate, cycleMonths)`, clamped to the last valid day of the resulting month (e.g. Jan 31 + 1 month -> Feb 28/29). This matches .NET's `DateTime.AddMonths` clamping behavior natively, so no custom calendar math is needed.
- **Alternative considered**: prorating partial first periods. Rejected per explicit decision - owner wants full-period charges regardless of move-in day, anchored to that day going forward.

### Due period generation timing: generate on demand, not via background job
Rather than running a scheduled job to pre-create every future due period, compute a resident's next due date lazily (derive it from move-in date + cycle + elapsed cycles) and materialize a due-period/payment-tracking record only when it becomes current or when the owner logs a payment against it. This avoids needing a scheduler/worker process for a single-owner, low-volume system.
- **Alternative considered**: nightly job pre-generating due periods. Rejected for v1 - adds infrastructure (background worker) for no behavioral benefit at this scale; can be revisited if reporting needs a materialized history.

### Checkout sequencing enforced at the domain layer
A resident must have a recorded notice date before checkout can complete (per `resident-management` spec). This is enforced as a state check in the domain/service layer (resident status must be "serving notice" before transitioning to "checked out"), not just UI-level validation, so the API itself rejects an out-of-order request.

### Expense categories as a fixed enum
Electricity / Wifi / Maintenance / Staff / Other is modeled as a fixed enum rather than an owner-configurable table, since the proposal specifies a predefined list. Keeps the model simple for v1; can become configurable later without a breaking data migration (the enum values can be seeded as rows if needed).

### Testing strategy: xUnit + Jest + Playwright
- **.NET unit/integration tests**: xUnit. Unit tests cover pure logic (due-date generation, overdue detection, expense totals). Integration tests use `WebApplicationFactory` against a real test database rather than mocking the data layer, since the tenancy-filter guarantee (every query scoped to `OwnerOrgId`) is only meaningful if verified against actual query execution, not a mock that bypasses it.
- **Angular unit tests**: Jest. Chosen over the Karma/Jasmine default since there is no existing Karma config in this scaffold to preserve, and Jest is faster for this size of project.
- **E2E tests**: Playwright, driving the real running app (server + SPA) through the core owner flows end-to-end: login/setup, hostel/room/cot creation, resident KYC + cot assignment, notice + checkout, payment logging, and dashboard verification. These are the acceptance-level checks that the six capabilities work together, not just in isolation.
- **Alternative considered**: Cypress for E2E. Rejected - the user asked for Playwright specifically.

## Risks / Trade-offs

- **Lazy due-period generation complexity** -> Mitigation: keep the "current due date" calculation as a pure function of (move-in date, cycle, today) so it's independently testable, rather than spreading the logic across endpoints.
- **Single shared schema means a tenancy-filter bug leaks data across owners** -> Mitigation: centralize the Owner Org filter (query interceptor/base repository) so it can't be forgotten per-endpoint; cover it with tests that assert cross-org access is denied.
- **Fixed expense-category enum limits flexibility if sold to owners with different cost structures** -> Mitigation: acceptable for v1 per explicit decision; revisit if/when sold to other owners with different needs.

## Migration Plan

This is a greenfield change on a scaffold with no existing data, so there is no data migration. Standard EF Core (or equivalent) initial migration creates all new tables. No rollback concerns beyond normal schema-migration practice.
