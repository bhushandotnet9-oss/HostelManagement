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
  /** The *agreed* deposit, not a record of money received - see depositCollected. */
  depositAmount: number;
  billingCycle: BillingCycle | null;
  noticeDate: string | null;
  noticePeriodDays: number | null;
  checkoutDate: string | null;
  depositRefundAmount: number | null;
  /** Total of the resident's deposit payments. */
  depositCollected: number;
  /** depositAmount minus depositCollected, never below zero. */
  depositOutstanding: number;
}

export interface DepositPayment {
  id: string;
  residentId: string;
  amount: number;
  paidDate: string;
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

  /**
   * Terms (rent, deposit, billing cycle) are optional: the server rejects them
   * for a resident with no assigned cot, so a plain KYC correction omits them.
   */
  update(
    residentId: string,
    name: string,
    contactNumber: string,
    aadhaarNumber: string,
    moveInDate: string,
    terms?: { rentAmount: number; depositAmount: number; billingCycle: BillingCycle },
  ): Observable<Resident> {
    return this.http.put<Resident>(`/api/residents/${residentId}`, {
      name,
      contactNumber,
      aadhaarNumber,
      moveInDate,
      rentAmount: terms?.rentAmount ?? null,
      depositAmount: terms?.depositAmount ?? null,
      billingCycle: terms?.billingCycle ?? null,
    });
  }

  remove(residentId: string): Observable<void> {
    return this.http.delete<void>(`/api/residents/${residentId}`);
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

  /**
   * Deposit payments hang off the resident, not a due period: the deposit is
   * owed from move-in rather than on the billing schedule.
   */
  logDepositPayment(residentId: string, amount: number, date: string): Observable<DepositPayment> {
    return this.http.post<DepositPayment>(`/api/residents/${residentId}/deposit-payments`, { amount, date });
  }

  listDepositPayments(residentId: string): Observable<DepositPayment[]> {
    return this.http.get<DepositPayment[]>(`/api/residents/${residentId}/deposit-payments`);
  }
}
