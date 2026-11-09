# Spec Delta

## ADDED Requirements

### Requirement: Payment type
Every logged payment SHALL carry a type of either Rent or Deposit. A Rent payment settles a resident's rent due period; a Deposit payment is recorded against the resident's agreed security deposit. The system SHALL treat a payment logged against a due period as a Rent payment.

#### Scenario: Rent payment is typed as rent
- **WHEN** the owner logs a payment against a resident's due period
- **THEN** the system records that payment with type Rent

#### Scenario: Deposit payment is typed as deposit
- **WHEN** the owner logs a deposit payment for a resident
- **THEN** the system records that payment with type Deposit

### Requirement: Deposit payment logging
The system SHALL allow the owner to record a Deposit payment for a resident, including amount and date paid, without reference to any due period. The system SHALL allow the deposit to be collected across multiple payments.

#### Scenario: Owner logs a deposit payment
- **WHEN** the owner records a deposit amount and date for a resident whose agreed deposit is greater than zero
- **THEN** the system saves the payment as a Deposit payment for that resident

#### Scenario: Deposit collected in instalments
- **WHEN** the owner records a second deposit payment for a resident who has already part-paid their deposit
- **THEN** the system saves it and the resident's collected deposit is the sum of both payments

#### Scenario: Deposit collected before any rent is due
- **WHEN** the owner records a deposit payment for a resident on their move-in date, before any due period has been generated
- **THEN** the system saves the payment without requiring a due period

#### Scenario: Deposit payment is rejected without an agreed deposit
- **WHEN** the owner attempts to record a deposit payment for a resident whose agreed deposit amount is zero or who is not assigned to a cot
- **THEN** the system rejects the payment and reports that no deposit is agreed for that resident

#### Scenario: Deposit over-collection is rejected
- **WHEN** the owner attempts to record a deposit payment that would make the total collected exceed the resident's agreed deposit amount
- **THEN** the system rejects the payment and reports the remaining outstanding deposit, leaving the collected total unchanged

#### Scenario: Deposit payment for another owner's resident is not found
- **WHEN** an owner attempts to record a deposit payment for a resident belonging to another owner org
- **THEN** the system reports the resident as not found

### Requirement: Deposit payments do not settle rent dues
A Deposit payment SHALL NOT change any due period's amount paid, paid status, or overdue status. Rent dues SHALL be settled only by Rent payments.

#### Scenario: Logging a deposit leaves an unpaid due period unpaid
- **WHEN** the owner records a deposit payment for a resident who has an unpaid due period
- **THEN** that due period's amount paid is unchanged and it remains unpaid

#### Scenario: Logging a deposit does not clear an overdue period
- **WHEN** the owner records a deposit payment for a resident with an overdue due period
- **THEN** that due period remains overdue

### Requirement: Deposit payment history
The system SHALL allow the owner to view the Deposit payments recorded for a resident, each with its amount and date paid, so the collected total can be traced to individual payments.

#### Scenario: Owner views a resident's deposit payments
- **WHEN** the owner requests the deposit payments for a resident who has made two deposit payments
- **THEN** the system lists both payments with their amounts and dates

#### Scenario: Resident with no deposit payments
- **WHEN** the owner requests the deposit payments for a resident who has paid no deposit
- **THEN** the system returns an empty list

## MODIFIED Requirements

### Requirement: Manual payment logging
The system SHALL allow the owner to record a Rent payment against a resident's due period, including amount and date paid. The system SHALL mark a due period as paid once Rent payments covering its full rent amount have been logged against it.

#### Scenario: Owner logs a payment
- **WHEN** the owner records a payment amount and date against a resident's current due period
- **THEN** the system saves the payment as a Rent payment and, if it covers the full rent amount, marks that due period as paid

#### Scenario: Partial payment leaves due period unpaid
- **WHEN** the owner records a payment less than the full rent amount for a due period
- **THEN** the system keeps that due period marked as unpaid and reflects the partial amount received
