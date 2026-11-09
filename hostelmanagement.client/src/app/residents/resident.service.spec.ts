import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ResidentService } from './resident.service';

describe('ResidentService update and delete', () => {
  let service: ResidentService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ResidentService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('PUTs a KYC-only correction with null terms', () => {
    service.update('res1', 'Ravi Kumar', '8888888888', '123412341234', '2026-01-15').subscribe();

    const request = http.expectOne('/api/residents/res1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      name: 'Ravi Kumar',
      contactNumber: '8888888888',
      aadhaarNumber: '123412341234',
      moveInDate: '2026-01-15',
      rentAmount: null,
      depositAmount: null,
      billingCycle: null,
    });
    request.flush({});
  });

  it('PUTs terms when they are supplied', () => {
    service
      .update('res1', 'Ravi Kumar', '9999999999', '123412341234', '2026-01-15', {
        rentAmount: 6000,
        depositAmount: 12000,
        billingCycle: 'Quarterly',
      })
      .subscribe();

    const request = http.expectOne('/api/residents/res1');
    expect(request.request.body).toMatchObject({
      rentAmount: 6000,
      depositAmount: 12000,
      billingCycle: 'Quarterly',
    });
    request.flush({});
  });

  it('DELETEs a resident', () => {
    service.remove('res1').subscribe();

    const request = http.expectOne('/api/residents/res1');
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
  });

  it('POSTs a deposit payment to the resident, not a due period', () => {
    service.logDepositPayment('res1', 4000, '2026-01-20').subscribe();

    const request = http.expectOne('/api/residents/res1/deposit-payments');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ amount: 4000, date: '2026-01-20' });
    request.flush({});
  });

  it('GETs a resident deposit payments', () => {
    service.listDepositPayments('res1').subscribe();

    const request = http.expectOne('/api/residents/res1/deposit-payments');
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });
});
