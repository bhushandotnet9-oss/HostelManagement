# Spec Delta

## ADDED Requirements

### Requirement: Resident KYC details can be corrected
The system SHALL allow the owner to correct an existing resident's name, contact number, Aadhaar number, and move-in date. All four SHALL remain required, with the three text fields non-blank, matching the rules applied when the record was created.

#### Scenario: Owner corrects a resident's contact number
- **WHEN** the owner changes a resident's contact number to a new non-blank value
- **THEN** the system saves the new contact number and returns the updated resident record

#### Scenario: Blanking a required KYC field is rejected
- **WHEN** the owner attempts to change a resident's Aadhaar number to an empty or whitespace-only value
- **THEN** the system rejects the change, reports the missing field, and leaves the resident record unchanged

#### Scenario: Another owner's resident cannot be changed
- **WHEN** an owner attempts to change a resident belonging to a different Owner Org
- **THEN** the system reports that the resident was not found and leaves the record unchanged

### Requirement: Assignment terms can be corrected
The system SHALL allow the owner to correct the rent amount, deposit amount, and billing cycle of a resident who is currently assigned to a cot. The system SHALL reject a terms correction for a resident with no assigned cot, since those terms are recorded at assignment time.

#### Scenario: Owner corrects a mistyped rent amount
- **WHEN** the owner changes the rent amount of a resident assigned to a cot
- **THEN** the system saves the new rent amount and returns the updated resident record

#### Scenario: Terms cannot be set for an unassigned resident
- **WHEN** the owner attempts to set a rent amount or billing cycle for a resident who has no assigned cot
- **THEN** the system rejects the change and reports that the resident must be assigned to a cot first

### Requirement: Move-in date is fixed once billing has started
The move-in date anchors a resident's entire billing schedule. The system SHALL allow the move-in date to be corrected only while the resident has no billing periods yet. Once any billing period exists, the system SHALL refuse a move-in date change and report that billing has already started, while still accepting corrections to the resident's other fields.

#### Scenario: Move-in date corrected before billing starts
- **WHEN** the owner corrects the move-in date of a resident who has no billing periods yet
- **THEN** the system saves the new move-in date

#### Scenario: Move-in date change refused after billing starts
- **WHEN** the owner attempts to change the move-in date of a resident who already has at least one billing period
- **THEN** the system refuses the change, reports that billing has already started, and leaves the resident record unchanged

#### Scenario: Other fields still correctable after billing starts
- **WHEN** the owner corrects the contact number of a resident who already has billing periods, leaving the move-in date as it is
- **THEN** the system saves the corrected contact number

### Requirement: Lifecycle state is not editable through correction
A correction SHALL NOT change a resident's status, cot assignment, notice date, notice period, checkout date, or deposit refund amount. Those fields SHALL change only through the existing assignment, notice, and checkout actions.

#### Scenario: Correction leaves lifecycle fields untouched
- **WHEN** the owner corrects the KYC details of a resident who is serving notice
- **THEN** the system saves the corrected details and leaves the resident's status, cot assignment, notice date, and notice period exactly as they were

### Requirement: Corrected terms apply only to future unpaid dues
When the owner corrects a resident's rent amount or billing cycle, the system SHALL apply the new terms to billing periods that are both unpaid and not yet due. Billing periods that already have a payment recorded, or whose due date has passed, SHALL keep their original amounts so billing history is never rewritten.

#### Scenario: Future unpaid due picks up the new rent
- **WHEN** the owner raises a resident's rent amount and the resident has an unpaid billing period whose due date is still in the future
- **THEN** that billing period's rent amount becomes the new rent amount

#### Scenario: Paid and past-due periods keep their original amounts
- **WHEN** the owner raises a resident's rent amount and the resident has an already-paid billing period and an unpaid overdue billing period
- **THEN** both of those periods keep their original rent amounts

### Requirement: Resident deletion requires no payment history
The system SHALL allow the owner to delete a resident record that has no recorded payments. If any payment has been recorded against the resident, the system SHALL refuse the deletion and report that payment history exists, without deleting anything.

#### Scenario: Owner deletes a resident entered by mistake
- **WHEN** the owner deletes a resident who has no recorded payments
- **THEN** the system removes the resident record along with its billing periods, and the resident no longer appears in the resident list

#### Scenario: Deleting an assigned resident frees the cot
- **WHEN** the owner deletes a resident with no recorded payments who is assigned to a cot
- **THEN** the system removes the resident and the cot's status becomes Vacant

#### Scenario: Resident with payment history cannot be deleted
- **WHEN** the owner attempts to delete a resident who has at least one recorded payment
- **THEN** the system refuses the deletion, reports that the resident has payment history, and leaves the resident, their billing periods, and their cot assignment in place
