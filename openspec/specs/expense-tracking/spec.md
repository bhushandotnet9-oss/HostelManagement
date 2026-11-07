# Expense Tracking Specification

## Purpose

Lets the owner record operating expenses against each hostel in predefined categories, so hostel-wise costs are visible alongside rent revenue.

## Requirements

### Requirement: Predefined expense categories
The system SHALL provide a predefined list of expense categories (Electricity, Wifi, Maintenance, Staff, Other) and SHALL require every expense entry to use one of them.

#### Scenario: Owner logs an expense with a predefined category
- **WHEN** the owner records an expense amount, date, and selects "Electricity" as the category
- **THEN** the system saves the expense under that category

#### Scenario: Owner logs an expense with no matching category
- **WHEN** the owner records an expense that does not fit Electricity, Wifi, Maintenance, or Staff
- **THEN** the system allows saving it under the "Other" category

### Requirement: Expense scoped to a hostel
Every expense entry SHALL belong to exactly one hostel.

#### Scenario: Owner records a hostel-specific expense
- **WHEN** the owner records an expense for a specific hostel
- **THEN** the system saves the expense against that hostel only

### Requirement: Expense totals by category
The system SHALL allow the owner to view total expenses for a hostel broken down by category over a given date range.

#### Scenario: Owner views expense breakdown
- **WHEN** the owner requests the expense breakdown for a hostel over a date range
- **THEN** the system returns the total amount spent per category within that range
