# Owner Dashboard Specification

## Purpose

Gives the owner a single aggregated view across their hostels — occupancy, dues, revenue, and expenses — so they don't need to inspect each hostel's records individually to understand their business at a glance.

## Requirements

### Requirement: Overdue residents list
The system SHALL allow the owner to view a list of all residents with at least one overdue due period, across all of the owner's hostels, including the hostel, cot, amount owed, and how many days overdue.

#### Scenario: Owner views overdue residents
- **WHEN** the owner opens the dashboard
- **THEN** the system lists every resident with an overdue due period, with hostel, cot, amount owed, and days overdue

### Requirement: Upcoming dues
The system SHALL allow the owner to view residents whose due date falls within a configurable number of upcoming days.

#### Scenario: Owner views upcoming dues
- **WHEN** the owner requests upcoming dues for the next 7 days
- **THEN** the system lists residents whose due date falls within that window

### Requirement: Occupancy summary across hostels
The system SHALL allow the owner to view occupancy percentage and vacant cot count for each hostel, and combined across all hostels.

#### Scenario: Owner views occupancy summary
- **WHEN** the owner opens the dashboard
- **THEN** the system shows occupancy percentage and vacant cot count per hostel and in total

### Requirement: Revenue collected vs expected
The system SHALL allow the owner to view, per hostel and for a given period, the total rent expected (from due periods falling in that period) versus the total rent actually collected (from logged payments).

#### Scenario: Owner views revenue summary
- **WHEN** the owner requests the revenue summary for a hostel for a given month
- **THEN** the system returns the total expected rent and the total collected rent for that hostel and month

### Requirement: Expense totals on dashboard
The system SHALL allow the owner to view total expenses by category for each hostel on the dashboard, consistent with the expense-tracking capability.

#### Scenario: Owner views expense totals on the dashboard
- **WHEN** the owner opens the dashboard
- **THEN** the system shows total expenses by category for each hostel for the selected period
