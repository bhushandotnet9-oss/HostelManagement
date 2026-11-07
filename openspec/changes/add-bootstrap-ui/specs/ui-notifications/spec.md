# Spec Delta

## Purpose

Gives the owner consistent, reachable navigation and clear feedback on the outcome of their actions across every page of the client app, instead of each page handling (or not handling) these concerns on its own.

## ADDED Requirements

### Requirement: Navigation available on every authenticated page
The system SHALL show navigation links to Dashboard, Hostels, Residents, and Expenses, and a Log out action, on every page the owner reaches after authenticating.

#### Scenario: Navigating away from the dashboard
- **WHEN** the owner is on the Hostels, Residents, or Expenses page
- **THEN** the navigation links and Log out action are visible and usable, without needing to use the browser's back button

#### Scenario: Logging out from any authenticated page
- **WHEN** the owner selects Log out from any authenticated page
- **THEN** the system ends the session and returns the owner to the Login page

### Requirement: Navigation hidden when not authenticated
The system SHALL NOT show the navigation shell on the Login or Setup pages.

#### Scenario: Viewing the login page
- **WHEN** the owner is on the Login or Setup page
- **THEN** no navigation links or Log out action are shown

### Requirement: Toast feedback on action outcome
The system SHALL show a toast notification confirming success or reporting failure whenever the owner submits a form that creates, updates, or records data (for example: login, setup, adding a hostel, room, or cot, assigning a cot, recording a notice, completing a checkout, logging a payment, or recording an expense).

#### Scenario: Successful submission
- **WHEN** the owner submits a form and the action completes successfully
- **THEN** the system shows a success toast and the toast dismisses on its own after a short delay

#### Scenario: Failed submission
- **WHEN** the owner submits a form and the action fails (validation error or request failure)
- **THEN** the system shows an error toast describing the failure, and the toast remains legible until dismissed or it times out

### Requirement: Toasts are non-blocking
The system SHALL display toast notifications without blocking or obscuring the form the owner is working in.

#### Scenario: Toast shown while a form stays usable
- **WHEN** a toast notification is visible
- **THEN** the owner can still see and interact with the page's primary content
