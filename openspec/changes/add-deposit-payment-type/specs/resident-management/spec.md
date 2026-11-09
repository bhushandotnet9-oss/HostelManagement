# Spec Delta

## ADDED Requirements

### Requirement: Deposit collected and outstanding
The system SHALL report, for each resident, the deposit collected (the total of that resident's Deposit payments) and the deposit outstanding (the agreed deposit amount minus the collected amount, never below zero), so the owner can see an uncollected or part-collected deposit.

#### Scenario: Resident has paid no deposit
- **WHEN** the owner views a resident with an agreed deposit of 10000 and no deposit payments
- **THEN** the system reports deposit collected of 0 and deposit outstanding of 10000

#### Scenario: Resident has part-paid their deposit
- **WHEN** the owner views a resident with an agreed deposit of 10000 who has paid 4000
- **THEN** the system reports deposit collected of 4000 and deposit outstanding of 6000

#### Scenario: Resident has fully paid their deposit
- **WHEN** the owner views a resident with an agreed deposit of 10000 who has paid the full 10000
- **THEN** the system reports deposit collected of 10000 and deposit outstanding of 0

#### Scenario: Agreed deposit is lowered below what was collected
- **WHEN** the owner lowers a resident's agreed deposit below the amount already collected
- **THEN** the system reports the full collected amount and a deposit outstanding of 0

### Requirement: Recorded payments block resident deletion
The system SHALL refuse to delete a resident who has any recorded payment, whether Rent or Deposit, and report that the resident has recorded payments.

#### Scenario: Resident with only a deposit payment cannot be deleted
- **WHEN** the owner attempts to delete a resident who has a recorded deposit payment and no rent payments
- **THEN** the system refuses the deletion and reports that the resident has recorded payments

#### Scenario: Resident with no payments can be deleted
- **WHEN** the owner deletes a resident who has no recorded payments of either type
- **THEN** the system deletes the resident, removes their due periods, and returns their cot to Vacant

## MODIFIED Requirements

### Requirement: Cot assignment
The system SHALL assign exactly one resident to exactly one vacant cot at a time, recording the resident's rent amount, agreed deposit amount, billing cycle, and move-in date at assignment time. The agreed deposit amount is the deposit owed, not a record of money received; what has actually been collected is derived from the resident's Deposit payments.

#### Scenario: Owner assigns a resident to a cot
- **WHEN** the owner assigns a resident to a vacant cot with a rent amount, agreed deposit amount, and billing cycle
- **THEN** the system records the assignment and the cot's status becomes Occupied

#### Scenario: Assignment alone does not collect the deposit
- **WHEN** the owner assigns a resident to a cot with an agreed deposit amount and logs no deposit payment
- **THEN** the system reports that resident's full agreed deposit as outstanding
