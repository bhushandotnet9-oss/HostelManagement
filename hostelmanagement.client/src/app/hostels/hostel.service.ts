import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface Hostel {
  id: string;
  name: string;
  location: string;
}

export interface Room {
  id: string;
  hostelId: string;
  name: string;
}

export interface Cot {
  id: string;
  roomId: string;
  label: string;
  status: 'Vacant' | 'Occupied';
}

export interface HostelOccupancy {
  total: number;
  occupied: number;
  vacant: number;
}

export interface RevenueSummary {
  expected: number;
  collected: number;
}

@Injectable({ providedIn: 'root' })
export class HostelService {
  constructor(private readonly http: HttpClient) {}

  listHostels(): Observable<Hostel[]> {
    return this.http.get<Hostel[]>('/api/hostels');
  }

  createHostel(name: string, location: string): Observable<Hostel> {
    return this.http.post<Hostel>('/api/hostels', { name, location });
  }

  occupancy(hostelId: string): Observable<HostelOccupancy> {
    return this.http.get<HostelOccupancy>(`/api/hostels/${hostelId}/occupancy`);
  }

  listRooms(hostelId: string): Observable<Room[]> {
    return this.http.get<Room[]>(`/api/hostels/${hostelId}/rooms`);
  }

  createRoom(hostelId: string, name: string): Observable<Room> {
    return this.http.post<Room>(`/api/hostels/${hostelId}/rooms`, { name });
  }

  listCots(roomId: string): Observable<Cot[]> {
    return this.http.get<Cot[]>(`/api/rooms/${roomId}/cots`);
  }

  createCot(roomId: string, label: string): Observable<Cot> {
    return this.http.post<Cot>(`/api/rooms/${roomId}/cots`, { label });
  }

  revenue(hostelId: string, from: string, to: string): Observable<RevenueSummary> {
    return this.http.get<RevenueSummary>(`/api/hostels/${hostelId}/revenue?from=${from}&to=${to}`);
  }
}
