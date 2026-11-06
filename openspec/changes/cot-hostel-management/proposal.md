# Proposal

## Why

The owner currently tracks cot-basis hostel occupancy, rent dues, deposits, and expenses across 2 Pune hostels manually (spreadsheets/notebooks), with no single view of vacancies, overdue residents, or per-hostel financials. This change replaces that manual process with a dedicated system, built multi-tenant from the start so it can later be offered to other hostel owners without a rework.

## What Changes

- Introduce an Owner Org as the top-level tenancy boundary; the owner is the sole user/login for their org (no caretaker or resident logins in this change).
- Model hostel structure: Hostel -> Room -> Cot, with each cot's occupancy status (vacant/occupied) tracked explicitly.
- Model residents with Aadhaar-based KYC, assignment to a specific cot, move-in date, rent amount, deposit amount paid, and a billing cycle (Monthly / Quarterly / Half-Yearly / Yearly).
- Generate rent due dates per resident by advancing their move-in day-of-month by their billing cycle's month count (no proration for partial periods).
- Allow manual logging of rent payments against due periods (no payment gateway integration); track paid/overdue status per resident.
- Support a checkout/vacate flow: record a notice date and notice period before vacating, then mark the cot vacant and record the deposit refund amount.
- Track hostel expenses against a predefined category list (electricity, wifi, maintenance, staff, etc.) plus an "Other" category, scoped per hostel.
- Provide an owner dashboard aggregating: overdue residents, upcoming dues (next N days), occupancy % / vacant cots per hostel, revenue collected vs. expected per hostel, and expense totals by category.
- Currency and ID-proof type are India-specific for this change (₹, Aadhaar) rather than configurable per country.

## Capabilities

### New Capabilities
- `account-management`: Owner Org as the tenancy boundary; owner login/session scoped to their org, with all other data isolated by org.
- `hostel-inventory`: Hostels, rooms, and cots, including each cot's current occupancy status.
- `resident-management`: Resident records (Aadhaar KYC, contact info), cot assignment, move-in, notice-period tracking, checkout/vacate, and deposit paid/refund amounts.
- `rent-billing`: Per-resident billing cycle configuration, due-date generation, manual payment logging, and paid/overdue status.
- `expense-tracking`: Per-hostel expense entries against a predefined category list with an "Other" fallback.
- `owner-dashboard`: Aggregated reporting views combining occupancy, dues, revenue, and expense data across the owner's hostels.

### Modified Capabilities
None — this is a greenfield change; no existing specs are being modified.

## Impact

- Affected code: entire `HostelManagement.Server` (.NET 10 Web API) and `hostelmanagement.client` (Angular) projects, currently at default scaffold state (e.g. `WeatherForecastController`) with no domain code.
- New data model and persistence layer for Owner Org, Hostel, Room, Cot, Resident, Payment, Expense entities, all scoped by Owner Org.
- New authentication for a single owner-per-org login (no roles/permissions beyond owner in this change).
- New API endpoints and Angular UI for hostel/room/cot management, resident lifecycle, rent billing, expense tracking, and the owner dashboard.
- No external payment gateway, SMS/WhatsApp, or notification integrations in this change.
