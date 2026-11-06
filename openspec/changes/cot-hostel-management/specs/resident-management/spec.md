# Spec Delta

## Purpose

Tracks each resident assigned to a cot — their identity/KYC details, move-in terms, and their lifecycle through notice and checkout — as the cot-basis billing unit of the system.

## ADDED Requirements

### Requirement: Resident KYC record
The system SHALL require, for each resident, a name, contact number, Aadhaar number, and move-in date before the resident can be assigned to a cot.

#### Scenario: Owner creates a resident record
- **WHEN** the owner enters a resident's name, contact number, Aadhaar number, and move-in date
- **THEN** the system saves the resident record under the owner's Owner Org

#### Scenario: Incomplete KYC is rejected
- **WHEN** the owner attempts to save a resident record missing the Aadhaar number
- **THEN** the system rejects the save and reports the missing field

### Requirement: Cot assignment
The system SHALL assign exactly one resident to exactly one vacant cot at a time, recording the resident's rent amount, deposit amount paid, billing cycle, and move-in date at assignment time.

#### Scenario: Owner assigns a resident to a cot
- **WHEN** the owner assigns a resident to a vacant cot with a rent amount, deposit amount, and billing cycle
- **THEN** the system records the assignment and the cot's status becomes Occupied

### Requirement: Notice period before checkout
The system SHALL allow the owner to record a notice date and notice period length for a resident before that resident vacates. The system SHALL NOT allow checkout to be completed without a recorded notice date.

#### Scenario: Owner records notice
- **WHEN** the owner records a notice date and notice period for an active resident
- **THEN** the system saves the notice and marks the resident as serving notice

#### Scenario: Checkout without notice is rejected
- **WHEN** the owner attempts to check out a resident who has no recorded notice date
- **THEN** the system rejects the checkout and reports that notice must be recorded first

### Requirement: Checkout and deposit refund
The system SHALL allow the owner to complete checkout for a resident serving notice by recording a deposit refund amount. On checkout completion, the system SHALL mark the resident's cot as Vacant and mark the resident as checked out.

#### Scenario: Owner completes checkout
- **WHEN** the owner completes checkout for a resident serving notice, recording a deposit refund amount
- **THEN** the system marks the resident as checked out, records the refund amount, and marks the resident's cot as Vacant

#### Scenario: Checked-out resident cannot be billed further
- **WHEN** the owner views dues or generates a new due date for a checked-out resident
- **THEN** the system does not generate further dues for that resident
