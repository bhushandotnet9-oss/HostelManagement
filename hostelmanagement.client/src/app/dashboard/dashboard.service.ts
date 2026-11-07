import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ExpenseCategoryTotal } from '../expenses/expense.service';

export interface OverdueResident {
  residentId: string;
  residentName: string;
  hostelId: string;
  hostelName: string;
  cotLabel: string;
  amountOwed: number;
  daysOverdue: number;
}

export interface UpcomingDue {
  residentId: string;
  residentName: string;
  hostelId: string;
  hostelName: string;
  dueDate: string;
  amountDue: number;
}

export interface HostelOccupancySummary {
  hostelId: string | null;
  hostelName: string | null;
  total: number;
  occupied: number;
  vacant: number;
  occupancyPercent: number;
}

export interface OccupancySummary {
  perHostel: HostelOccupancySummary[];
  combined: HostelOccupancySummary;
}

export interface HostelExpenseTotals {
  hostelId: string;
  hostelName: string;
  totals: ExpenseCategoryTotal[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly http: HttpClient) {}

  overdue(): Observable<OverdueResident[]> {
    return this.http.get<OverdueResident[]>('/api/dashboard/overdue');
  }

  upcomingDues(days: number): Observable<UpcomingDue[]> {
    return this.http.get<UpcomingDue[]>(`/api/dashboard/upcoming-dues?days=${days}`);
  }

  occupancy(): Observable<OccupancySummary> {
    return this.http.get<OccupancySummary>('/api/dashboard/occupancy');
  }

  expenseTotals(from: string, to: string): Observable<HostelExpenseTotals[]> {
    return this.http.get<HostelExpenseTotals[]>(`/api/dashboard/expenses?from=${from}&to=${to}`);
  }
}
