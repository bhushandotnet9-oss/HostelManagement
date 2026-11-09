import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { ToastService } from '../shared/toast.service';
import { Resident } from './resident.service';
import { ResidentsComponent } from './residents.component';

const unassigned: Resident = {
  id: 'res1',
  name: 'Ravi Kumar',
  contactNumber: '9999999999',
  aadhaarNumber: '123412341234',
  moveInDate: '2026-01-15',
  status: 'Active',
  cotId: null,
  rentAmount: 0,
  depositAmount: 0,
  billingCycle: null,
  noticeDate: null,
  noticePeriodDays: null,
  checkoutDate: null,
  depositRefundAmount: null,
  depositCollected: 0,
  depositOutstanding: 0,
};

const assigned: Resident = {
  ...unassigned,
  id: 'res2',
  cotId: 'cot1',
  rentAmount: 5000,
  depositAmount: 10000,
  billingCycle: 'Monthly',
  depositCollected: 0,
  depositOutstanding: 10000,
};

describe('ResidentsComponent edit and delete', () => {
  let fixture: ComponentFixture<ResidentsComponent>;
  let component: ResidentsComponent;
  let http: HttpTestingController;
  let toast: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ResidentsComponent],
      imports: [FormsModule],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ResidentsComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);

    // ngOnInit loads residents and walks hostels to find vacant cots.
    fixture.detectChanges();
    http.expectOne('/api/residents').flush([unassigned, assigned]);
    http.expectOne('/api/hostels').flush([]);
  });

  afterEach(() => http.verify());

  it('offers terms only for an assigned resident', () => {
    component.startEditResident(unassigned);
    expect(component.editHasCot).toBe(false);

    component.startEditResident(assigned);
    expect(component.editHasCot).toBe(true);
    expect(component.editRentAmount).toBe(5000);
    expect(component.editBillingCycle).toBe('Monthly');
  });

  it('sends a KYC-only correction for an unassigned resident', () => {
    component.startEditResident(unassigned);
    component.editContactNumber = '8888888888';

    component.saveResident(unassigned);

    const request = http.expectOne('/api/residents/res1');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toMatchObject({
      contactNumber: '8888888888',
      rentAmount: null,
      billingCycle: null,
    });
    request.flush(unassigned);

    http.expectOne('/api/residents').flush([unassigned, assigned]);

    expect(component.editingResidentId()).toBeNull();
    expect(toast.toasts()[0].kind).toBe('success');
  });

  it('sends terms for an assigned resident', () => {
    component.startEditResident(assigned);
    component.editRentAmount = 6000;

    component.saveResident(assigned);

    const request = http.expectOne('/api/residents/res2');
    expect(request.request.body).toMatchObject({
      rentAmount: 6000,
      depositAmount: 10000,
      billingCycle: 'Monthly',
    });
    request.flush(assigned);

    http.expectOne('/api/residents').flush([unassigned, assigned]);
  });

  it('shows the server message when the move-in date can no longer change', () => {
    component.startEditResident(assigned);
    component.editMoveInDate = '2026-03-01';

    component.saveResident(assigned);

    http
      .expectOne('/api/residents/res2')
      .flush('Billing has already started for this resident, so the move-in date can no longer be changed.', {
        status: 409,
        statusText: 'Conflict',
      });

    const entry = toast.toasts()[0];
    expect(entry.kind).toBe('error');
    expect(entry.message).toContain('move-in date can no longer be changed');
    // The row stays in edit mode so the owner can correct the date back.
    expect(component.editingResidentId()).toBe('res2');
  });

  it('does not save a correction that blanks a required field', () => {
    component.startEditResident(assigned);
    component.editAadhaarNumber = '  ';

    component.saveResident(assigned);

    http.expectNone('/api/residents/res2');
  });

  it('issues no request when the delete confirmation is declined', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(false);

    component.deleteResident(assigned);

    http.expectNone('/api/residents/res2');
  });

  it('shows the server message when a resident delete is blocked by payments', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(true);

    component.deleteResident(assigned);

    http.expectOne('/api/residents/res2').flush('This resident has recorded payments and cannot be deleted.', {
      status: 409,
      statusText: 'Conflict',
    });

    const entry = toast.toasts()[0];
    expect(entry.kind).toBe('error');
    expect(entry.message).toBe('This resident has recorded payments and cannot be deleted.');
  });

  it('deletes a resident on confirmation and reloads', () => {
    jest.spyOn(window, 'confirm').mockReturnValue(true);

    component.deleteResident(assigned);

    const remove = http.expectOne('/api/residents/res2');
    expect(remove.request.method).toBe('DELETE');
    remove.flush(null);

    http.expectOne('/api/residents').flush([unassigned]);
    http.expectOne('/api/hostels').flush([]);

    expect(component.residents().length).toBe(1);
    expect(toast.toasts()[0].kind).toBe('success');
  });

  it('logs a deposit payment against the resident and refreshes the totals', () => {
    component.depositPaymentAmount['res2'] = 4000;
    component.depositPaymentDate['res2'] = '2026-01-20';

    component.logDepositPayment(assigned);

    const request = http.expectOne('/api/residents/res2/deposit-payments');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ amount: 4000, date: '2026-01-20' });
    request.flush({});

    // Collected and outstanding are derived server-side, so the list is refetched.
    http
      .expectOne('/api/residents')
      .flush([unassigned, { ...assigned, depositCollected: 4000, depositOutstanding: 6000 }]);

    expect(component.residents()[1].depositCollected).toBe(4000);
    expect(component.residents()[1].depositOutstanding).toBe(6000);
    expect(component.depositPaymentAmount['res2']).toBe(0);
    expect(component.depositPaymentDate['res2']).toBe('');
    expect(toast.toasts()[0].kind).toBe('success');
  });

  it('shows the server message when a deposit payment over-collects', () => {
    component.depositPaymentAmount['res2'] = 20000;
    component.depositPaymentDate['res2'] = '2026-01-20';

    component.logDepositPayment(assigned);

    http
      .expectOne('/api/residents/res2/deposit-payments')
      .flush('That exceeds the agreed deposit; only 10000 remains outstanding.', {
        status: 409,
        statusText: 'Conflict',
      });

    const entry = toast.toasts()[0];
    expect(entry.kind).toBe('error');
    expect(entry.message).toBe('That exceeds the agreed deposit; only 10000 remains outstanding.');
    // The entered values stay put so the owner can correct the amount.
    expect(component.depositPaymentAmount['res2']).toBe(20000);
  });

  it('issues no request for an incomplete deposit payment form', () => {
    component.depositPaymentAmount['res2'] = 4000;
    component.depositPaymentDate['res2'] = '';

    component.logDepositPayment(assigned);

    http.expectNone('/api/residents/res2/deposit-payments');
  });
});
