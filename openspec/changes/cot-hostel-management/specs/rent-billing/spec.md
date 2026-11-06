# Spec Delta

## Purpose

Generates each resident's rent due dates according to their own billing cycle and tracks manually logged payments against those dues, so the owner always knows who has paid and who is overdue.

## ADDED Requirements

### Requirement: Per-resident billing cycle
Each resident SHALL have a billing cycle of Monthly, Quarterly, Half-Yearly, or Yearly, corresponding to 1, 3, 6, or 12 months respectively, set at the time they are assigned to a cot.

#### Scenario: Owner sets billing cycle at assignment
- **WHEN** the owner assigns a resident to a cot and selects a billing cycle
- **THEN** the system stores that billing cycle for the resident

### Requirement: Due date generation
The system SHALL generate each resident's first due date as their move-in date, and each subsequent due date by advancing the prior due date by the resident's billing cycle's month count, preserving the original day-of-month. The system SHALL NOT prorate charges for any period regardless of the day of month the resident moved in.

#### Scenario: Monthly resident's due dates advance by one month
- **WHEN** a resident with a Monthly cycle moved in on the 15th
- **THEN** the system generates due dates on the 15th of each subsequent month

#### Scenario: Quarterly resident's due dates advance by three months
- **WHEN** a resident with a Quarterly cycle moved in on the 10th
- **THEN** the system generates due dates on the 10th every 3 months

#### Scenario: Due date falls on a short month
- **WHEN** a resident moved in on the 31st and their next calculated due date falls in a month with fewer than 31 days
- **THEN** the system sets the due date to the last day of that month

### Requirement: Manual payment logging
The system SHALL allow the owner to record a payment against a resident's due period, including amount and date paid. The system SHALL mark a due period as paid once a payment covering its full rent amount has been logged against it.

#### Scenario: Owner logs a payment
- **WHEN** the owner records a payment amount and date against a resident's current due period
- **THEN** the system saves the payment and, if it covers the full rent amount, marks that due period as paid

#### Scenario: Partial payment leaves due period unpaid
- **WHEN** the owner records a payment less than the full rent amount for a due period
- **THEN** the system keeps that due period marked as unpaid and reflects the partial amount received

### Requirement: Overdue status
The system SHALL mark a resident's due period as overdue when its due date has passed without being marked paid.

#### Scenario: Due period becomes overdue
- **WHEN** a due date passes without a payment covering the full rent amount
- **THEN** the system marks that due period as overdue for the resident
