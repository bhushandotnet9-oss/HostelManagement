# Spec Delta

## ADDED Requirements

### Requirement: Individual expense entries are listable
The system SHALL allow the owner to view the individual expense entries recorded against a hostel, each identifiable so it can be selected for correction or removal. This is distinct from the category breakdown, which reports totals only.

#### Scenario: Owner views a hostel's expense entries
- **WHEN** the owner requests the expense entries for a hostel
- **THEN** the system returns each entry with its identifier, category, amount, date, and notes

#### Scenario: Owner views a single expense entry
- **WHEN** the owner requests one specific expense entry of their own hostel
- **THEN** the system returns that entry's category, amount, date, and notes

### Requirement: Expense entries can be corrected
The system SHALL allow the owner to change a recorded expense's category, amount, date, and notes. The amount SHALL remain positive and the category SHALL remain one of the predefined categories. An expense SHALL NOT be moved to a different hostel.

#### Scenario: Owner corrects an expense amount
- **WHEN** the owner changes a recorded expense's amount to a new positive value
- **THEN** the system saves the new amount and the hostel's category breakdown reflects it

#### Scenario: Owner recategorizes an expense
- **WHEN** the owner changes a recorded expense's category from Electricity to Maintenance
- **THEN** the system saves the new category and the breakdown moves that amount from Electricity to Maintenance

#### Scenario: Non-positive amount is rejected
- **WHEN** the owner attempts to change a recorded expense's amount to zero or a negative value
- **THEN** the system rejects the change, reports that the amount must be positive, and leaves the expense unchanged

#### Scenario: Another owner's expense cannot be changed
- **WHEN** an owner attempts to change an expense belonging to a different Owner Org
- **THEN** the system reports that the expense was not found and leaves it unchanged

### Requirement: Expense entries can be deleted
The system SHALL allow the owner to delete any recorded expense of their own. Nothing depends on an expense entry, so the deletion SHALL never be refused for referential reasons.

#### Scenario: Owner deletes an expense entered by mistake
- **WHEN** the owner deletes a recorded expense
- **THEN** the system removes the entry, it no longer appears in the hostel's expense entries, and its amount is excluded from the category breakdown

#### Scenario: Another owner's expense cannot be deleted
- **WHEN** an owner attempts to delete an expense belonging to a different Owner Org
- **THEN** the system reports that the expense was not found and leaves it in place
