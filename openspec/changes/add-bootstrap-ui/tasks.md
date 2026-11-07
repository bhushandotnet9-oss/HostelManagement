# Tasks

## 1. Bootstrap dependency

- [x] 1.1 Add `bootstrap` to `hostelmanagement.client/package.json` and run `npm install`; verify `node_modules/bootstrap` exists and `npm run build` still succeeds.
- [x] 1.2 Wire `bootstrap/dist/css/bootstrap.min.css` and `bootstrap/dist/js/bootstrap.bundle.min.js` into `angular.json`'s `styles`/`scripts` arrays; verify `ng serve` renders a Bootstrap-styled element (e.g. a `btn` class visibly styled) on any page.

## 2. Toast service and container

- [x] 2.1 Implement `ToastService` in `hostelmanagement.client/src/app/shared/toast.service.ts` with `success(message: string)` / `error(message: string)` methods backed by a `signal<ToastEntry[]>`; verify with a unit test that calling `success`/`error` adds an entry with the right kind and message.
- [x] 2.2 Implement `ToastContainerComponent` (`src/app/shared/toast-container.component.*`) that renders one Bootstrap `.toast` per entry, shows it via `bootstrap.Toast`, and removes the entry on `hidden.bs.toast`; verify by rendering the component in a test with a queued entry and asserting the toast element appears, and manually in the browser that it auto-dismisses.
- [x] 2.3 Register `<app-toast-container />` once in `app.html`; verify a toast triggered from any page appears above all page content (fixed bottom-right) without shifting page layout.

## 3. Navigation shell

- [x] 3.1 Implement `NavShellComponent` (`src/app/shared/nav-shell.component.*`) with a Bootstrap `navbar` containing links to Dashboard/Hostels/Residents/Expenses and a Log out button wired to the existing logout logic; verify logout still redirects to `/login`.
- [x] 3.2 Show `<app-nav-shell />` in `app.html` only when `AuthService` reports authenticated, hide it otherwise; verify manually that Login/Setup show no shell and Dashboard/Hostels/Residents/Expenses all show it.
- [x] 3.3 Remove the now-redundant inline `<nav>` and logout button from `dashboard.component.html` (superseded by the shared shell); verify the dashboard page has no duplicate nav/logout controls.

## 4. Toast integration on existing forms

- [x] 4.1 Call `ToastService.success`/`.error` in `login.component.ts` and `setup.component.ts` subscribe callbacks; remove the inline `error()` signal and `<p class="error">` markup; verify a failed login/setup shows an error toast and a successful one navigates away.
- [x] 4.2 Call `ToastService.success`/`.error` in `hostels.component.ts` for create-hostel, create-room, and create-cot; verify each action shows a toast on both success and failure (e.g. by forcing a failing request in dev tools).
- [x] 4.3 Call `ToastService.success`/`.error` in `residents.component.ts` for create-resident, assign-cot, record-notice, checkout, and log-payment; verify each shows a toast on both success and failure.
- [x] 4.4 Call `ToastService.success`/`.error` in `expenses.component.ts` for create-expense; verify it shows a toast on both success and failure.

## 5. Page restyling

- [x] 5.1 Restyle `login.component.html` and `setup.component.html` as a centered Bootstrap `card` (max-width ~400px) with `form-label`/`form-control`/`btn btn-primary`; verify visually in the browser at desktop and mobile widths.
- [x] 5.2 Restyle `dashboard.component.html` with a `container` wrapper, each section as a `card`, and all `<table>` elements as `table table-striped table-hover`; verify all existing data (overdue, upcoming dues, occupancy, revenue, expenses) still renders correctly.
- [x] 5.3 Restyle `hostels.component.html`: `card` for the add-hostel form, `list-group` for the hostel list, `card`/`table` for the selected hostel's rooms/cots section; verify create-hostel/room/cot flows still work end-to-end.
- [x] 5.4 Restyle `residents.component.html`: `card` per resident with a status `badge` (Active/ServingNotice/CheckedOut), Bootstrap forms for assign-cot/notice/checkout/payment actions, `table table-striped` for the dues table; verify all resident actions still work end-to-end.
- [x] 5.5 Restyle `expenses.component.html`: `list-group` for the hostel picker, `card` for the add-expense and breakdown-range forms, `table table-striped` for the breakdown table; verify create-expense and breakdown flows still work end-to-end.

## 6. Verification

- [x] 6.1 Run the existing Jest unit tests (`npm test` in `hostelmanagement.client`) and verify they pass with the restyled templates.
- [x] 6.2 Run the Playwright e2e suite (`npm test` in `e2e`, which rebuilds and copies `dist` into `wwwroot`) and verify all existing scenarios still pass against the restyled app.
- [x] 6.3 Manually walk through login -> setup -> dashboard -> hostels -> residents -> expenses -> logout in the browser and verify navigation is reachable from every authenticated page and every create/update action produces a toast.
