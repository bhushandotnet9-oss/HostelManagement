import { Component, OnInit, signal } from '@angular/core';
import { apiErrorMessage } from '../shared/api-error';
import { ToastService } from '../shared/toast.service';
import { Cot, Hostel, HostelOccupancy, HostelService, Room } from './hostel.service';

@Component({
  selector: 'app-hostels',
  standalone: false,
  templateUrl: './hostels.component.html',
})
export class HostelsComponent implements OnInit {
  readonly hostels = signal<Hostel[]>([]);
  readonly selectedHostel = signal<Hostel | null>(null);
  readonly occupancy = signal<HostelOccupancy | null>(null);
  readonly rooms = signal<Room[]>([]);
  readonly cotsByRoom = signal<Record<string, Cot[]>>({});

  newHostelName = '';
  newHostelLocation = '';
  newRoomName = '';
  newCotLabelByRoom: Record<string, string> = {};

  // One editing id per entity kind: a row swaps itself for an inline form while
  // its id is the editing one. Only one row of each kind is editable at a time.
  readonly editingHostelId = signal<string | null>(null);
  readonly editingRoomId = signal<string | null>(null);
  readonly editingCotId = signal<string | null>(null);

  editHostelName = '';
  editHostelLocation = '';
  editRoomName = '';
  editCotLabel = '';

  constructor(
    private readonly hostelService: HostelService,
    private readonly toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.reloadHostels();
  }

  reloadHostels(): void {
    this.hostelService.listHostels().subscribe((hostels) => this.hostels.set(hostels));
  }

  createHostel(): void {
    if (!this.newHostelName.trim() || !this.newHostelLocation.trim()) {
      return;
    }
    this.hostelService.createHostel(this.newHostelName, this.newHostelLocation).subscribe({
      next: () => {
        this.newHostelName = '';
        this.newHostelLocation = '';
        this.reloadHostels();
        this.toast.success('Hostel added successfully.');
      },
      error: () => this.toast.error('Failed to add hostel.'),
    });
  }

  selectHostel(hostel: Hostel): void {
    this.selectedHostel.set(hostel);
    this.rooms.set([]);
    this.cotsByRoom.set({});
    this.loadOccupancy(hostel.id);
    this.loadRooms(hostel.id);
  }

  loadOccupancy(hostelId: string): void {
    this.hostelService.occupancy(hostelId).subscribe((occupancy) => this.occupancy.set(occupancy));
  }

  loadRooms(hostelId: string): void {
    this.hostelService.listRooms(hostelId).subscribe((rooms) => {
      this.rooms.set(rooms);
      rooms.forEach((room) => this.loadCots(room.id));
    });
  }

  loadCots(roomId: string): void {
    this.hostelService.listCots(roomId).subscribe((cots) => {
      this.cotsByRoom.update((current) => ({ ...current, [roomId]: cots }));
    });
  }

  createRoom(): void {
    const hostel = this.selectedHostel();
    if (!hostel || !this.newRoomName.trim()) {
      return;
    }
    this.hostelService.createRoom(hostel.id, this.newRoomName).subscribe({
      next: () => {
        this.newRoomName = '';
        this.loadRooms(hostel.id);
        this.toast.success('Room added successfully.');
      },
      error: () => this.toast.error('Failed to add room.'),
    });
  }

  createCot(room: Room): void {
    const label = this.newCotLabelByRoom[room.id];
    if (!label?.trim()) {
      return;
    }
    this.hostelService.createCot(room.id, label).subscribe({
      next: () => {
        this.newCotLabelByRoom[room.id] = '';
        this.loadCots(room.id);
        const hostel = this.selectedHostel();
        if (hostel) {
          this.loadOccupancy(hostel.id);
        }
        this.toast.success('Cot added successfully.');
      },
      error: () => this.toast.error('Failed to add cot.'),
    });
  }

  startEditHostel(hostel: Hostel): void {
    this.editingHostelId.set(hostel.id);
    this.editHostelName = hostel.name;
    this.editHostelLocation = hostel.location;
  }

  cancelEditHostel(): void {
    this.editingHostelId.set(null);
  }

  saveHostel(hostel: Hostel): void {
    if (!this.editHostelName.trim() || !this.editHostelLocation.trim()) {
      return;
    }
    this.hostelService.updateHostel(hostel.id, this.editHostelName, this.editHostelLocation).subscribe({
      next: (updated) => {
        this.editingHostelId.set(null);
        this.reloadHostels();
        if (this.selectedHostel()?.id === hostel.id) {
          this.selectedHostel.set(updated);
        }
        this.toast.success('Hostel updated successfully.');
      },
      error: (error) => this.toast.error(apiErrorMessage(error, 'Failed to update hostel.')),
    });
  }

  deleteHostel(hostel: Hostel): void {
    if (!confirm(`Delete hostel "${hostel.name}"? This cannot be undone.`)) {
      return;
    }
    this.hostelService.deleteHostel(hostel.id).subscribe({
      next: () => {
        if (this.selectedHostel()?.id === hostel.id) {
          this.selectedHostel.set(null);
          this.rooms.set([]);
          this.cotsByRoom.set({});
          this.occupancy.set(null);
        }
        this.reloadHostels();
        this.toast.success('Hostel deleted successfully.');
      },
      error: (error) => this.toast.error(apiErrorMessage(error, 'Failed to delete hostel.')),
    });
  }

  startEditRoom(room: Room): void {
    this.editingRoomId.set(room.id);
    this.editRoomName = room.name;
  }

  cancelEditRoom(): void {
    this.editingRoomId.set(null);
  }

  saveRoom(room: Room): void {
    if (!this.editRoomName.trim()) {
      return;
    }
    this.hostelService.updateRoom(room.hostelId, room.id, this.editRoomName).subscribe({
      next: () => {
        this.editingRoomId.set(null);
        this.loadRooms(room.hostelId);
        this.toast.success('Room updated successfully.');
      },
      error: (error) => this.toast.error(apiErrorMessage(error, 'Failed to update room.')),
    });
  }

  deleteRoom(room: Room): void {
    if (!confirm(`Delete room "${room.name}"? This cannot be undone.`)) {
      return;
    }
    this.hostelService.deleteRoom(room.hostelId, room.id).subscribe({
      next: () => {
        this.loadRooms(room.hostelId);
        this.loadOccupancy(room.hostelId);
        this.toast.success('Room deleted successfully.');
      },
      error: (error) => this.toast.error(apiErrorMessage(error, 'Failed to delete room.')),
    });
  }

  startEditCot(cot: Cot): void {
    this.editingCotId.set(cot.id);
    this.editCotLabel = cot.label;
  }

  cancelEditCot(): void {
    this.editingCotId.set(null);
  }

  saveCot(cot: Cot): void {
    if (!this.editCotLabel.trim()) {
      return;
    }
    this.hostelService.updateCot(cot.roomId, cot.id, this.editCotLabel).subscribe({
      next: () => {
        this.editingCotId.set(null);
        this.loadCots(cot.roomId);
        this.toast.success('Cot updated successfully.');
      },
      error: (error) => this.toast.error(apiErrorMessage(error, 'Failed to update cot.')),
    });
  }

  deleteCot(cot: Cot): void {
    if (!confirm(`Delete cot "${cot.label}"? This cannot be undone.`)) {
      return;
    }
    this.hostelService.deleteCot(cot.roomId, cot.id).subscribe({
      next: () => {
        this.loadCots(cot.roomId);
        const hostel = this.selectedHostel();
        if (hostel) {
          this.loadOccupancy(hostel.id);
        }
        this.toast.success('Cot deleted successfully.');
      },
      error: (error) => this.toast.error(apiErrorMessage(error, 'Failed to delete cot.')),
    });
  }
}
