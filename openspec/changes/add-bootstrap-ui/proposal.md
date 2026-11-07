# Proposal

## Why

Every page in the Angular client (login, setup, dashboard, hostels, residents, expenses) renders as bare, unstyled HTML. There is no shared navigation shell — the nav links and logout button live only inside the dashboard template, so once an owner navigates to Hostels, Residents, or Expenses they have no way back except the browser's back button, and no way to log out. Form submissions give little to no feedback: only login/setup show an inline error paragraph, and successful actions (adding a hostel, logging a payment, recording an expense) produce no acknowledgement at all. This makes the app hard to use day-to-day.

## What Changes

- Add the `bootstrap` npm package (CSS + bundled JS) to `hostelmanagement.client` and import it globally via `angular.json`/`styles.css`. No other new third-party UI library.
- Add a shared app shell (navbar) shown whenever the owner is authenticated, with links to Dashboard, Hostels, Residents, Expenses, and a Log out action, visible on every authenticated route.
- Add a hand-rolled Angular `ToastService` (wrapping Bootstrap's `bootstrap.Toast`) plus a toast container rendered once at the app root, used to confirm success and surface errors on every form submission across the app.
- Replace the existing inline `<p class="error">` feedback on login/setup with the same toast mechanism, for one consistent feedback pattern app-wide.
- Restyle all six page templates (login, setup, dashboard, hostels, residents, expenses) with Bootstrap 5 layout primitives (`container`, `card`, `table`, `form-control`, `btn`, `badge`, `list-group`, `nav`) — no behavioral changes to the underlying forms/data beyond the toast feedback above.

## Capabilities

### New Capabilities
- `ui-notifications`: Global navigation availability (nav + logout reachable from every authenticated page) and toast-based feedback on the outcome of user actions.

### Modified Capabilities
(none — existing capabilities describe business behavior only; this change does not alter any of their requirements)

## Impact

- `hostelmanagement.client/package.json`: new dependency `bootstrap`.
- `hostelmanagement.client/angular.json`, `src/styles.css`: global Bootstrap CSS/JS wiring.
- `hostelmanagement.client/src/app/app.html`, `app.ts`, `app-module.ts`: new shared shell + toast container, `ToastService` registration.
- New `hostelmanagement.client/src/app/shared/` files: `toast.service.ts`, `toast-container.component.*`, `nav-shell.component.*` (or equivalent).
- All six existing page templates (`auth/login.component.html`, `auth/setup.component.html`, `dashboard/dashboard.component.html`, `hostels/hostels.component.html`, `residents/residents.component.html`, `expenses/expenses.component.html`) and their `.ts` files where they need to call the new `ToastService`.
- No backend/API changes.
