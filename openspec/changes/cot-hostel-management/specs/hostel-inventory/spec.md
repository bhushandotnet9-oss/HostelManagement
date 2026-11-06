# Spec Delta

## Purpose

Models the physical structure of an owner's hostels down to the individually billable cot, and tracks each cot's occupancy status so the owner can see vacancies at a glance across all their hostels.

## ADDED Requirements

### Requirement: Hostel, room, and cot hierarchy
The system SHALL allow an owner to create hostels, and within each hostel create rooms, and within each room create cots. Each cot SHALL belong to exactly one room, and each room SHALL belong to exactly one hostel.

#### Scenario: Owner adds a new hostel
- **WHEN** the owner creates a new hostel with a name and location
- **THEN** the system saves the hostel under the owner's Owner Org

#### Scenario: Owner adds rooms and cots
- **WHEN** the owner adds a room to a hostel and then adds cots to that room
- **THEN** the system saves each cot as belonging to that room and hostel

### Requirement: Cot occupancy status
Each cot SHALL have an occupancy status of either Vacant or Occupied. The system SHALL set a cot to Occupied when a resident is assigned to it, and back to Vacant when that resident checks out.

#### Scenario: Cot becomes occupied on move-in
- **WHEN** a resident is assigned to a vacant cot
- **THEN** the system marks that cot's status as Occupied

#### Scenario: Cot becomes vacant on checkout
- **WHEN** a resident assigned to a cot completes checkout
- **THEN** the system marks that cot's status as Vacant

#### Scenario: Occupied cot cannot be double-booked
- **WHEN** the owner attempts to assign a resident to a cot that is already Occupied
- **THEN** the system rejects the assignment

### Requirement: Occupancy overview per hostel
The system SHALL allow the owner to view, for any hostel, the total number of cots, how many are Occupied, and how many are Vacant.

#### Scenario: Owner views hostel occupancy
- **WHEN** the owner requests the occupancy overview for a hostel
- **THEN** the system returns the total, occupied, and vacant cot counts for that hostel
