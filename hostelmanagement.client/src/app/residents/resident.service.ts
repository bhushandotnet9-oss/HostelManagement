import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export type ResidentStatus = 'Active' | 'ServingNotice' | 'CheckedOut';
export type BillingCycle = 'Monthly' | 'Quarterly' | 'HalfYearly' | 'Yearly';

export interface Resident {
  id: string;
  name: string;
  contactNumber: string;
  aadhaarNumber: string;
  moveInDate: string;
  status: ResidentStatus;
  cotId: string | null;
  rentAmount: number;
  depositAmount: number;
  billingCycle: BillingCycle | null;
  noticeDate: string | null;
  noticePeriodDays: number | null;
  checkoutDate: string | null;
  depositRefundAmount: number | null;
}

@Injectable({ providedIn: 'root' })
export class ResidentService {
  constructor(private readonly http: HttpClient) {}

  list(): Observable<Resident[]> {
    return this.http.get<Resident[]>('/api/residents');
  }

  create(name: string, contactNumber: string, aadhaarNumber: string, moveInDate: string): Observable<Resident> {
    return this.http.post<Resident>('/api/residents', { name, contactNumber, aadhaarNumber, moveInDate });
  }

  assignCot(
    residentId: string,
    cotId: string,
    rentAmount: number,
    depositAmount: number,
    billingCycle: BillingCycle,
    moveInDate: string,
  ): Observable<Resident> {
    return this.http.post<Resident>(`/api/residents/${residentId}/assign-cot`, {
      cotId,
      rentAmount,
      depositAmount,
      billingCycle,
      moveInDate,
    });
  }

  recordNotice(residentId: string, noticeDate: string, noticePeriodDays: number): Observable<Resident> {
    return this.http.post<Resident>(`/api/residents/${residentId}/notice`, { noticeDate, noticePeriodDays });
  }

  checkout(residentId: string, depositRefundAmount: number): Observable<Resident> {
    return this.http.post<Resident>(`/api/residents/${residentId}/checkout`, { depositRefundAmount });
  }
}
