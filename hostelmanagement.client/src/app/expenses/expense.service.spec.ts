import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ExpenseService } from './expense.service';

describe('ExpenseService list, update and delete', () => {
  let service: ExpenseService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ExpenseService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('GETs the expense entries for a hostel', () => {
    service.list('h1').subscribe();

    const request = http.expectOne('/api/hostels/h1/expenses');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('PUTs an expense update', () => {
    service.update('h1', 'e1', 'Maintenance', 1250, '2026-01-10', 'Corrected').subscribe();

    const request = http.expectOne('/api/hostels/h1/expenses/e1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      category: 'Maintenance',
      amount: 1250,
      date: '2026-01-10',
      notes: 'Corrected',
    });
    request.flush({});
  });

  it('DELETEs an expense', () => {
    service.remove('h1', 'e1').subscribe();

    const request = http.expectOne('/api/hostels/h1/expenses/e1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });
});
