# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

A multi-tenant hostel/PG management system: ASP.NET Core 10 Web API backend (`HostelManagement.Server`) with Entity Framework Core + SQLite, and an Angular 22 frontend (`hostelmanagement.client`). The Angular app is normally served by the SpaProxy during development and built into the server's `wwwroot` for production/e2e. Feature work is tracked via OpenSpec (`openspec/`) using spec-driven change proposals.

## Commands

### Backend (HostelManagement.Server)
- Run API + SPA proxy: `dotnet run --project HostelManagement.Server` (or run `HostelManagement.slnx` from Visual Studio)
- Build: `dotnet build HostelManagement.slnx`
- Apply/create EF Core migrations: `dotnet ef migrations add <Name> --project HostelManagement.Server` / `dotnet ef database update --project HostelManagement.Server` (migrations also auto-apply on startup via `Database.Migrate()` in `Program.cs`)
- Run all server tests: `dotnet test HostelManagement.Server.Tests`
- Run a single test: `dotnet test HostelManagement.Server.Tests --filter "FullyQualifiedName~RentBillingTests.MethodName"`

### Frontend (hostelmanagement.client)
- Install deps: `npm install` (run inside `hostelmanagement.client`)
- Dev server: `npm start` (wraps `ng serve` with HTTPS cert setup via `aspnetcore-https.js`)
- Build: `npm run build` (output goes to `dist/hostelmanagement.client`)
- Unit tests (Jest): `npm test` — to run a single test file/spec, use `npx jest path/to/file.spec.ts` or `npx jest -t "test name"`

### End-to-end (e2e, Playwright)
- `npm test` inside `e2e/` — the `pretest` script builds the Angular app and copies the output into `HostelManagement.Server/wwwroot`, so e2e runs against the real built SPA served by the ASP.NET backend, not `ng serve`.
- Run a single spec: `npx playwright test tests/rent-billing.spec.ts`

### OpenSpec
This repo uses the experimental OpenSpec workflow (`opsx:*` skills/commands) for proposing, applying, syncing, and archiving spec-driven changes. Current specs live under `openspec/specs/<capability>/spec.md` (account-management, expense-tracking, hostel-inventory, owner-dashboard, rent-billing, resident-management); in-flight changes live under `openspec/changes/`.

## Architecture

### Multi-tenancy (central concept)
Every tenant-owned entity implements `ITenantScoped` (`OwnerOrgId` property). `AppDbContext` (`HostelManagement.Server/Data/AppDbContext.cs`):
- Applies a global EF Core query filter per tenant-scoped entity type via reflection in `OnModelCreating`, scoping all reads to `OwnerOrgIdForFilter`.
- Automatically stamps `OwnerOrgId` on newly added entities in `SaveChanges`/`SaveChangesAsync`, so controllers never set it manually.
- `ICurrentOwnerContext` (implemented by `HttpCurrentOwnerContext`) resolves the authenticated owner's `OwnerOrgId` from the current HTTP context; if there is no authenticated owner it's `Guid.Empty`, which deliberately matches zero rows ("fail closed" rather than leaking cross-tenant data).
- Tests substitute `MutableOwnerContext` (via `CustomWebApplicationFactory`) to simulate different owners/orgs without going through real auth.

When adding a new entity that belongs to an owner org, implement `ITenantScoped` — the filtering and stamping is automatic from there.

### Domain model
`Owner` → `OwnerOrg` (an owner's organization) → `Hostel` → `Room` → `Cot` → `Resident`. A `Resident` occupies a `Cot` (one-to-one, `Resident.CotId`). Billing: `DuePeriod` rows represent individual billing periods owed by a resident, paid via `Payment`. `Expense` tracks hostel operating costs. Enums (`Models/Enums.cs`): `CotStatus`, `BillingCycle` (Monthly/Quarterly/HalfYearly/Yearly, valued as month counts), `ResidentStatus` (Active/ServingNotice/CheckedOut), `ExpenseCategory`.

### Billing engine (`HostelManagement.Server/Billing`)
- `DueDateCalculator` is pure date math: advances due dates by a resident's billing cycle using `DateOnly.AddMonths` (which clamps to end-of-month, so the day-of-month anchor can drift after a clamp — this is accepted by design, not a bug).
- `DuePeriodMaterializer` lazily creates missing `DuePeriod` rows up to "today" (on read or before logging a payment) rather than via a background job. A `CheckedOut` resident stops accruing further periods even if past-due dates would otherwise apply.

### Auth
JWT bearer auth (`Auth/TokenService.cs`, `Auth/JwtOptions.cs`), configured in `Program.cs` from the `Jwt` config section (required — startup throws if missing). `AccountController` handles signup/login/setup. The Angular side mirrors this with `auth.guard.ts` (route protection) and `auth.interceptor.ts` (attaches the token to outgoing requests).

### API surface
Controllers under `Controllers/` map closely to domain areas: `AccountController`, `HostelsController`, `RoomsController`, `CotsController`, `ResidentsController`, `DuesController`, `ExpensesController`, `DashboardController`. Request/response shapes live in `Controllers/Dtos`. `GET /api/health` is an unauthenticated liveness check mapped directly in `Program.cs`.

The five owner-facing resources (hostels, rooms, cots, residents, expenses) expose update (`PUT`) and delete (`DELETE`) alongside read and create. Two conventions hold across all of them:
- **Deletes are guarded, not cascading.** A record with dependents is refused with `409 Conflict` and a message naming the blocker (a hostel with rooms/residents/expenses, a room with cots, an `Occupied` cot, a resident with recorded payments). The one exception is deleting a resident, which also removes their derived `DuePeriod` rows and returns their cot to `Vacant` — payments are what block it, not dues, since merely reading a resident's dues materializes them.
- **Another org's record is `404`, not `403`.** The tenant query filter means the controller genuinely cannot see the row, so cross-tenant requests are indistinguishable from missing ones by design.

Resident updates cover KYC and assignment terms only; `Status`, `CotId`, notice, and checkout fields change solely through the existing lifecycle actions. The move-in date is frozen with a `409` once any `DuePeriod` exists, because it anchors the whole billing schedule (see `Billing/DueDateCalculator`).

### Static file serving
`wwwroot` is populated by directly copying the Angular `dist/.../browser` build output (see `e2e/package.json`'s `pretest`), not through the SpaProxy/esproj manifest pipeline. Because of this, `Program.cs` uses `UseStaticFiles()` rather than `MapStaticAssets()` — the latter expects a build manifest that isn't present here and would otherwise serve `.js` files with an empty content type.

### Frontend structure
Angular app (`hostelmanagement.client/src/app`) is organized by feature folder, each typically pairing a `*.component.ts`/`.html` with a `*.service.ts` for API calls: `auth/`, `dashboard/`, `expenses/`, `hostels/`, `residents/`. `shared/` holds cross-cutting UI (`nav-shell`, toast notifications via `toast.service.ts` + `toast-container.component`). Styled with Bootstrap 5.

### Testing conventions
Server integration tests (`HostelManagement.Server.Tests`) use `CustomWebApplicationFactory` (a `WebApplicationFactory<Program>`) against a real per-test-run SQLite file in temp storage, with `MutableOwnerContext` swapped in to control the "current owner" for multi-tenant scenarios without real auth. Test classes are organized by feature (e.g. `RentBillingTests`, `HostelInventoryTests`, `ExpenseTrackingTests`), mirroring the OpenSpec capability names.
