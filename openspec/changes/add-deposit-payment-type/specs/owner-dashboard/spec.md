# Spec Delta

## MODIFIED Requirements

### Requirement: Revenue collected vs expected
The system SHALL allow the owner to view, per hostel and for a given period, the total rent expected (from due periods falling in that period) versus the total rent actually collected (from logged Rent payments). Deposit payments SHALL NOT count towards collected revenue.

#### Scenario: Owner views revenue summary
- **WHEN** the owner requests the revenue summary for a hostel for a given month
- **THEN** the system returns the total expected rent and the total collected rent for that hostel and month

#### Scenario: Deposit payments are excluded from collected revenue
- **WHEN** a resident paid a deposit within the requested period and paid no rent
- **THEN** the system reports collected rent of 0 for that resident's contribution to the hostel's total
