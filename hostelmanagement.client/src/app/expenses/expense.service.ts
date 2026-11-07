import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export type ExpenseCategory = 'Electricity' | 'Wifi' | 'Maintenance' | 'Staff' | 'Other';

export interface Expense {
  id: string;
  hostelId: string;
  category: ExpenseCategory;
  amount: number;
  date: string;
  notes: string | null;
}

export interface ExpenseCategoryTotal {
  category: ExpenseCategory;
  total: number;
}

@Injectable({ providedIn: 'root' })
export class ExpenseService {
  constructor(private readonly http: HttpClient) {}

  create(hostelId: string, category: ExpenseCategory, amount: number, date: string, notes: string): Observable<Expense> {
    return this.http.post<Expense>(`/api/hostels/${hostelId}/expenses`, { category, amount, date, notes });
  }

  breakdown(hostelId: string, from: string, to: string): Observable<ExpenseCategoryTotal[]> {
    return this.http.get<ExpenseCategoryTotal[]>(
      `/api/hostels/${hostelId}/expenses/breakdown?from=${from}&to=${to}`,
    );
  }
}
