# Spec Delta

## Purpose

Establishes the Owner Org as the tenancy boundary for the system and provides the owner with a single login scoped to their own org's data, so multiple hostel owners can one day use the same system without seeing each other's data.

## ADDED Requirements

### Requirement: Owner Org as tenancy boundary
Every hostel, room, cot, resident, payment, and expense record SHALL belong to exactly one Owner Org. The system SHALL NOT return or modify data belonging to an Owner Org other than the one the authenticated owner belongs to.

#### Scenario: Owner sees only their own org's data
- **WHEN** an authenticated owner requests any hostel, resident, payment, or expense data
- **THEN** the system returns only records belonging to that owner's Owner Org

#### Scenario: Cross-org access is denied
- **WHEN** an authenticated owner attempts to access a record belonging to a different Owner Org (e.g. by guessing an identifier)
- **THEN** the system denies the request and does not reveal whether the record exists

### Requirement: Single owner login per org
The system SHALL support exactly one owner account per Owner Org for authentication. The system SHALL NOT provide caretaker, staff, or resident login accounts in this change.

#### Scenario: Owner logs in
- **WHEN** the owner submits valid credentials for their account
- **THEN** the system authenticates them and scopes their session to their Owner Org

#### Scenario: Invalid credentials are rejected
- **WHEN** a login attempt uses incorrect credentials
- **THEN** the system rejects the attempt and does not create a session

### Requirement: Owner Org created at setup
The system SHALL create an Owner Org together with its owner account as a single setup step, so a new owner can begin recording hostels immediately after account creation.

#### Scenario: New owner sets up their org
- **WHEN** a new owner completes account setup
- **THEN** the system creates an Owner Org for them and allows them to begin creating hostels under it
