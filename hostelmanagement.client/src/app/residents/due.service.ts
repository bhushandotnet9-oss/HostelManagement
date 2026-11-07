import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export interface DuePeriod {
  id: string;
  residentId: string;
  dueDate: string;
  rentAmount: number;
  amountPaid: number;
  isPaid: boolean;
  isOverdue: boolean;
}

@Injectable({ providedIn: 'root' })
export class DueService {
  constructor(private readonly http: HttpClient) {}

  listForResident(residentId: string): Observable<DuePeriod[]> {
    return this.http.get<DuePeriod[]>(`/api/residents/${residentId}/dues`);
  }

  logPayment(duePeriodId: string, amount: number, date: string): Observable<DuePeriod> {
    return this.http.post<DuePeriod>(`/api/dues/${duePeriodId}/payments`, { amount, date });
  }
}
