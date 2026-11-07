import { Component, OnInit, signal } from '@angular/core';
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

  constructor(private readonly hostelService: HostelService) {}

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
    this.hostelService.createHostel(this.newHostelName, this.newHostelLocation).subscribe(() => {
      this.newHostelName = '';
      this.newHostelLocation = '';
      this.reloadHostels();
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
    this.hostelService.createRoom(hostel.id, this.newRoomName).subscribe(() => {
      this.newRoomName = '';
      this.loadRooms(hostel.id);
    });
  }

  createCot(room: Room): void {
    const label = this.newCotLabelByRoom[room.id];
    if (!label?.trim()) {
      return;
    }
    this.hostelService.createCot(room.id, label).subscribe(() => {
      this.newCotLabelByRoom[room.id] = '';
      this.loadCots(room.id);
      const hostel = this.selectedHostel();
      if (hostel) {
        this.loadOccupancy(hostel.id);
      }
    });
  }
}
