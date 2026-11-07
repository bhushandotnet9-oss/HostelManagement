# Design

## Context

`hostelmanagement.client` is an Angular 22 app, module-based (`standalone: false`), with `AppModule` declaring all components and `AppRoutingModule` guarding `dashboard`/`hostels`/`residents`/`expenses` behind `authGuard`. `src/styles.css` is empty today - no CSS framework, no design tokens. Each page component currently owns its entire template with bare HTML (`<form>`, `<table>`, `<ul>`) and inline `[(ngModel)]` bindings; there is no shared layout component and `app.html` is just `<router-outlet>`. See proposal.md - Why for the navigation/feedback gaps this leaves.

## Goals / Non-Goals

**Goals:**
- Wire Bootstrap 5 in once, globally, with a single new dependency (`bootstrap`).
- Add one shared nav shell and one shared toast mechanism, reused by all six pages.
- Restyle all six existing templates to use Bootstrap layout primitives, without changing the data/behavior each page already has (same fields, same API calls).

**Non-Goals:**
- No new Angular UI component library (`ng-bootstrap`, `ngx-bootstrap`, Angular Material, CDK) - rejected specifically to keep the dependency count minimal, per proposal.md.
- No migration to standalone components or a routing/state rewrite - this change only touches templates, styles, and the new shell/toast pieces.
- No change to any backend/API contract.
- No visual design system (tokens, theming) beyond Bootstrap's defaults - out of scope for this change.

## Decisions

### Bootstrap wiring: npm package, global import, no Angular wrapper
Add `bootstrap` to `hostelmanagement.client/package.json`. Import `bootstrap/dist/css/bootstrap.min.css` in `angular.json`'s `styles` array (or `@import` in `styles.css`) and `bootstrap/dist/js/bootstrap.bundle.min.js` in `angular.json`'s `scripts` array, so it's available globally without per-component imports.

Alternatives considered: `ng-bootstrap`/`ngx-bootstrap` (Angular-native components, but a whole extra dependency tree for what is otherwise one JS class usage - toasts); Angular CDK overlay (one more dependency, more code, for the same result). Rejected both to honor "keep third-party libraries minimum."

### Toast mechanism: hand-rolled `ToastService` over `bootstrap.Toast`
A single `ToastService` (injectable, in `src/app/shared/`) exposes `success(message: string)` and `error(message: string)`. It pushes entries into a `signal<ToastEntry[]>`; a single `ToastContainerComponent` (rendered once in `app.html`, fixed-position bottom-right) iterates that signal and renders a Bootstrap `.toast` element per entry, instantiating `new bootstrap.Toast(el)` and calling `.show()` on it via `AfterViewInit`/`effect`, removing the entry from the signal on Bootstrap's `hidden.bs.toast` event.

Alternatives considered: a pure-CSS hand-rolled toast (no `bootstrap.Toast` class, just `position-fixed` + CSS transition) - rejected because it reimplements auto-dismiss timing and accessibility behavior that Bootstrap's JS already provides for free once `bootstrap.bundle.min.js` is loaded anyway (needed for the navbar's collapse behavior on mobile).

### Navigation shell: new `NavShellComponent`, driven by auth state
A new `NavShellComponent` (`src/app/shared/nav-shell.component.*`) renders a Bootstrap `navbar` with links to Dashboard/Hostels/Residents/Expenses and a Log out button. `app.html` becomes:

```html
@if (authService.isAuthenticated()) {
  <app-nav-shell />
}
<router-outlet></router-outlet>
<app-toast-container />
```

`isAuthenticated()` is derived from the existing `AuthService` (already backs `authGuard`), so no new auth state is introduced - the shell just reads the same signal/state the guard already uses. Login/Setup routes render with no shell because the owner isn't authenticated yet on those pages.

Alternative considered: give each guarded route its own layout wrapper component in the router config (`children` with a shared parent). Rejected as more routing-config churn for the same visible result; the signal-driven `@if` in `app.html` is the smaller change given the current flat route list.

### Per-page restyle approach
Each of the six templates gets Bootstrap classes applied directly (`container`/`container-fluid` wrapper, `card` for grouped forms/sections, `table table-striped table-hover` for existing tables, `form-label`/`form-control`/`form-select` for inputs, `btn btn-primary`/`btn-outline-secondary` for actions, `badge` for resident status, `list-group`/`list-group-item` for the hostel/resident lists, `nav-tabs` or `list-group` for the hostel picker on Expenses). Component `.ts` logic is touched only to call `ToastService.success(...)`/`.error(...)` after each existing `.subscribe(...)` call (both success and error callbacks) - no new fields, no new API calls.

## Risks / Trade-offs

- [Bootstrap's JS depends on Popper for some components (dropdowns/tooltips), pulled in by `bootstrap.bundle.min.js`] → Not a problem here: toasts and the navbar's mobile collapse don't need Popper, and `bootstrap.bundle.min.js` bundles it anyway, so there's no separate Popper dependency to add or manage.
- [Hand-rolled `ToastService` means less polish than a dedicated library (e.g. stacking limits, swipe-to-dismiss)] → Acceptable: proposal explicitly prioritizes minimum dependencies over polish; behavior required by specs/ui-notifications (non-blocking, auto-dismiss, legible until dismissed) is all Bootstrap's `Toast` class already provides.
- [Restyling six templates by hand risks visual inconsistency between pages] → Mitigated by reusing the same small set of Bootstrap component classes (card/table/form/btn/badge) across all six rather than inventing per-page custom CSS.

## Migration Plan

No data migration. Rollout is a single client-side change: add the dependency, add the shell/toast pieces, restyle templates. No feature flag - Bootstrap CSS/JS loading globally is safe to ship in one step since it only adds classes/behavior, it doesn't remove any existing markup structure that other code depends on. Rollback is reverting the commit(s); no backend or data impact.
