# Tasks

## 1. Foundation & Tenancy

- [x] 1.1 Remove scaffold template code (`WeatherForecastController.cs`, `WeatherForecast.cs`) and verify the server builds and starts with no references to them
- [x] 1.2 Add EF Core (or equivalent) data access layer with an `OwnerOrgId` column on every domain table and a centralized query filter/interceptor that scopes all reads/writes to the authenticated owner's org; verify with a test that a query without an explicit org filter still cannot return another org's row
- [x] 1.3 Create initial database migration for the OwnerOrg table and verify it applies cleanly to a fresh database
- [x] 1.4 Add an xUnit test project for the server with a `WebApplicationFactory`-based integration test fixture against a real test database, and verify a trivial integration test runs against it
- [x] 1.5 Switch the Angular project's unit test runner to Jest and verify a trivial component test runs via `npm test`
- [x] 1.6 Add a Playwright project configured to drive the running server + SPA together, and verify a trivial smoke test (load the app root) passes

## 2. Account Management (`account-management`)

- [x] 2.1 Implement OwnerOrg + Owner account entities and a setup endpoint that creates both together; verify a new owner can complete setup and immediately create a hostel under their org
- [x] 2.2 Implement owner login (credential validation, session/token issuance) and verify invalid credentials are rejected without creating a session
- [x] 2.3 Implement the cross-org denial check as an integration test: authenticate as owner A, attempt to access owner B's data by id, verify the request is denied and does not leak existence
- [x] 2.4 Build the Angular login/setup screens and verify an owner can sign up and log in end-to-end against the API

## 3. Hostel Inventory (`hostel-inventory`)

- [x] 3.1 Implement Hostel, Room, and Cot entities with the hierarchy (cot -> room -> hostel) and CRUD endpoints; verify with tests that a cot cannot be created without a valid room/hostel
- [x] 3.2 Implement cot occupancy status (Vacant/Occupied) with transitions driven by resident assignment/checkout (stubbed until section 4 lands); verify a cot defaults to Vacant on creation
- [x] 3.3 Implement the occupancy overview endpoint (total/occupied/vacant counts per hostel) and verify it returns correct counts for a seeded hostel
- [x] 3.4 Build Angular screens for managing hostels, rooms, and cots, and viewing per-hostel occupancy counts; verify end-to-end against the API

## 4. Resident Management (`resident-management`)

- [x] 4.1 Implement Resident entity with required KYC fields (name, contact number, Aadhaar number, move-in date) and verify saving a resident missing Aadhaar is rejected
- [x] 4.2 Implement cot assignment (resident + rent amount + deposit amount + billing cycle + move-in date) that flips the cot to Occupied, and reject assignment to an already-Occupied cot; verify both paths with tests
- [x] 4.3 Implement notice recording (notice date + notice period) and reject checkout attempts for residents with no recorded notice; verify both paths with tests
- [x] 4.4 Implement checkout (deposit refund amount) that marks the resident checked out and the cot Vacant, and verify a checked-out resident generates no further dues (coordinate with section 5)
- [x] 4.5 Build Angular screens for resident KYC entry, cot assignment, notice recording, and checkout; verify end-to-end against the API

## 5. Rent Billing (`rent-billing`)

- [x] 5.1 Implement the due-date calculation as a pure function of (move-in date, billing cycle months, today), using month-add with end-of-month clamping; verify with unit tests covering Monthly/Quarterly/Half-Yearly/Yearly cycles and a 31st-of-month edge case
- [x] 5.2 Implement due-period materialization on demand (when current or when a payment is logged) and verify no due periods are generated for checked-out residents
- [x] 5.3 Implement manual payment logging (amount, date) against a due period, marking it paid only when the full rent amount is covered, and verify partial payments leave the period unpaid
- [x] 5.4 Implement overdue detection for unpaid due periods past their due date and verify with a test using a past due date and no payment
- [x] 5.5 Build Angular screens for logging payments and viewing a resident's due/payment history; verify end-to-end against the API

## 6. Expense Tracking (`expense-tracking`)

- [x] 6.1 Implement the expense category enum (Electricity, Wifi, Maintenance, Staff, Other) and Expense entity scoped to a hostel; verify an expense cannot be saved without a valid category or hostel
- [x] 6.2 Implement the expense breakdown-by-category endpoint for a hostel and date range; verify totals sum correctly across multiple entries and categories
- [x] 6.3 Build Angular screens for logging expenses and viewing the category breakdown per hostel; verify end-to-end against the API

## 7. Owner Dashboard (`owner-dashboard`)

- [x] 7.1 Implement the overdue-residents endpoint (hostel, cot, amount owed, days overdue) across all of the owner's hostels; verify with a seeded overdue resident
- [x] 7.2 Implement the upcoming-dues endpoint with a configurable day window; verify residents just inside and just outside the window are included/excluded correctly
- [x] 7.3 Implement the occupancy summary endpoint (per hostel and combined) and verify percentages and vacant counts match seeded data
- [x] 7.4 Implement the revenue-expected-vs-collected endpoint for a hostel and period; verify expected comes from due periods in that period and collected comes from logged payments
- [x] 7.5 Implement the per-hostel expense totals-by-category endpoint on the dashboard, reusing section 6's breakdown logic; verify consistency with the expense-tracking breakdown
- [x] 7.6 Build the Angular dashboard screen combining overdue list, upcoming dues, occupancy summary, revenue summary, and expense totals; verify it renders correctly against seeded data for both of the owner's hostels

## 8. End-to-End Testing (Playwright)

- [x] 8.1 Write a Playwright test for owner setup + login, and verify it passes against the running server + SPA
- [x] 8.2 Write a Playwright test for creating a hostel, room, and cot and seeing it reflected in the occupancy overview, and verify it passes
- [x] 8.3 Write a Playwright test for adding a resident, assigning them to a cot, and seeing the cot flip to Occupied, and verify it passes
- [x] 8.4 Write a Playwright test for logging a rent payment and seeing the due period marked paid, and verify it passes
- [x] 8.5 Write a Playwright test for recording notice and completing checkout, and seeing the cot flip back to Vacant, and verify it passes
- [x] 8.6 Write a Playwright test for logging an expense and seeing it reflected in the hostel's category breakdown, and verify it passes
- [x] 8.7 Write a Playwright test that seeds overdue/upcoming dues and expenses across both hostels and verifies the owner dashboard shows correct aggregates, and verify it passes

## Workflow follow-up

- Archive this change once all sections above are implemented and verified.
- Re-verify the full dashboard and resident lifecycle end-to-end against the archived spec before considering the change complete.
