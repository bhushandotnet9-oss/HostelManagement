# Spec Delta

## ADDED Requirements

### Requirement: Hostel details can be corrected
The system SHALL allow the owner to change an existing hostel's name and location. Both SHALL remain required and non-blank. The system SHALL reject an attempt to change a hostel that does not belong to the owner's Owner Org as if the hostel did not exist.

#### Scenario: Owner renames a hostel
- **WHEN** the owner changes a hostel's name and location to new non-blank values
- **THEN** the system saves the new name and location and returns the updated hostel

#### Scenario: Blank name is rejected
- **WHEN** the owner attempts to change a hostel's name to an empty or whitespace-only value
- **THEN** the system rejects the change and reports that name and location are required

#### Scenario: Another owner's hostel cannot be changed
- **WHEN** an owner attempts to change a hostel belonging to a different Owner Org
- **THEN** the system reports that the hostel was not found and leaves it unchanged

### Requirement: Room details can be corrected
The system SHALL allow the owner to change an existing room's name within its hostel. The name SHALL remain required and non-blank. A room SHALL NOT be moved to a different hostel.

#### Scenario: Owner renames a room
- **WHEN** the owner changes a room's name to a new non-blank value
- **THEN** the system saves the new name and returns the updated room

#### Scenario: Blank room name is rejected
- **WHEN** the owner attempts to change a room's name to an empty or whitespace-only value
- **THEN** the system rejects the change and reports that the name is required

### Requirement: Cot label can be corrected
The system SHALL allow the owner to change an existing cot's label within its room. The label SHALL remain required and non-blank. The cot's occupancy status SHALL NOT be editable directly — it changes only through resident assignment and checkout.

#### Scenario: Owner relabels a cot
- **WHEN** the owner changes a cot's label to a new non-blank value
- **THEN** the system saves the new label and returns the updated cot with its occupancy status unchanged

#### Scenario: Blank cot label is rejected
- **WHEN** the owner attempts to change a cot's label to an empty or whitespace-only value
- **THEN** the system rejects the change and reports that the label is required

### Requirement: Hostel deletion requires an empty hostel
The system SHALL allow the owner to delete a hostel that has no rooms, no residents, and no recorded expenses. If any of those exist, the system SHALL refuse the deletion and report what is blocking it, without deleting anything.

#### Scenario: Owner deletes an empty hostel
- **WHEN** the owner deletes a hostel that has no rooms, residents, or expenses
- **THEN** the system removes the hostel and it no longer appears in the owner's hostel list

#### Scenario: Hostel with rooms cannot be deleted
- **WHEN** the owner attempts to delete a hostel that still has at least one room
- **THEN** the system refuses the deletion, reports that the hostel still has rooms, and leaves the hostel and its rooms in place

#### Scenario: Hostel with recorded expenses cannot be deleted
- **WHEN** the owner attempts to delete a room-free hostel that still has recorded expenses
- **THEN** the system refuses the deletion, reports that the hostel still has expenses, and leaves both in place

### Requirement: Room deletion requires an empty room
The system SHALL allow the owner to delete a room that has no cots. If the room still has cots, the system SHALL refuse the deletion and report that the room still has cots, without deleting anything.

#### Scenario: Owner deletes an empty room
- **WHEN** the owner deletes a room that has no cots
- **THEN** the system removes the room and it no longer appears in the hostel's room list

#### Scenario: Room with cots cannot be deleted
- **WHEN** the owner attempts to delete a room that still has at least one cot
- **THEN** the system refuses the deletion, reports that the room still has cots, and leaves the room and its cots in place

### Requirement: Cot deletion requires a vacant cot
The system SHALL allow the owner to delete a cot whose status is Vacant. The system SHALL refuse to delete an Occupied cot and report that a resident is assigned to it, so a resident is never silently detached from their cot.

#### Scenario: Owner deletes a vacant cot
- **WHEN** the owner deletes a cot whose status is Vacant
- **THEN** the system removes the cot, it no longer appears in the room's cot list, and the hostel's occupancy totals drop by one

#### Scenario: Occupied cot cannot be deleted
- **WHEN** the owner attempts to delete a cot whose status is Occupied
- **THEN** the system refuses the deletion, reports that a resident is assigned to the cot, and leaves the cot and the assignment in place
