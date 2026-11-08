import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { Hostel } from '../hostels/hostel.service';
import { ToastService } from '../shared/toast.service';
import { Expense } from './expense.service';
import { ExpensesComponent } from './expenses.component';

const hostel: Hostel = { id: 'h1', name: 'Hostel One', location: 'Pune' };
const expense: Expense = {
  id: 'e1',
  hostelId: 'h1',
  category: 'Electricity',
  amount: 1000,
  date: '2026-01-10',
  notes: null,
};

describe('ExpensesComponent entries, edit and delete', () => {
  let fixture: ComponentFixture<ExpensesComponent>;
  let component: ExpensesComponent;
  let http: HttpTestingController;
  let toast: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ExpensesComponent],
      imports: [FormsModule],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(ExpensesComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
    toast = TestBed.inject(ToastService);

    fixture.detectChanges();
    http.expectOne('/api/hostels').flush([hostel]);
  });

  afterEach(() => http.verify());

  it('loads the hostel expense entries on selection', () => {
    component.selectHostel(hostel);

    const request = http.expectOne('/api/hostels/h1/expenses');
    expect(request.request.method).toBe('GET');
    request.flush([expense]);

    expect(component.expenses()).toEqual([expense]);
  });

  it('enters edit mode prefilled from the entry', () => {
    component.selectHostel(hostel);
    http.expectOne('/api/hostels/h1/expenses').flush([expense]);

    component.startEditExpense(expense);

    expect(component.editingExpenseId()).toBe('e1');
    expect(component.editCategory).toBe('Electricity');
    expect(component.editAmount).toBe(1000);
    expect(component.editDate).toBe('2026-01-10');
  });

  it('saves an edit and refreshes the entries and breakdown', () => {
    component.selectHostel(hostel);
    http.expectOne('/api/hostels/h1/expenses').flush([expense]);
    component.rangeFrom = '2026-01-01';
    component.rangeTo = '2026-01-31';

    component.startEditExpense(expense);
    component.editAmount = 1250;
    component.saveExpense(expense);

    const update = http.expectOne('/api/hostels/h1/expenses/e1');
    expect(update.request.method).toBe('PUT');
    update.flush({ ...expense, amount: 1250 });

    http.expectOne('/api/hostels/h1/expenses').flush([{ ...expense, amount: 1250 }]);
    http
      .expectOne('/api/hostels/h1/expenses/breakdown?from=2026-01-01&to=2026-01-31')
      .flush([{ category: 'Electricity', total: 1250 }]);

    expect(component.editingExpenseId()).toBeNull();
    expect(component.expenses()[0].amount).toBe(1250);
    expect(component.breakdown()[0].total).toBe(1250);
    expect(toast.toasts()[0].kind).toBe('success');
  });

  it('does not save an edit with a non-positive amount', () => {
    component.selectHostel(hostel);
    http.expectOne('/api/hostels/h1/expenses').flush([expense]);

    component.startEditExpense(expense);
    component.editAmount = 0;
    component.saveExpense(expense);

    http.expectNone('/api/hostels/h1/expenses/e1');
  });

  it('issues no request when the delete confirmation is declined', () => {
    component.selectHostel(hostel);
    http.expectOne('/api/hostels/h1/expenses').flush([expense]);

    jest.spyOn(window, 'confirm').mockReturnValue(false);
    component.deleteExpense(expense);

    http.expectNone('/api/hostels/h1/expenses/e1');
  });

  it('removes the row on a confirmed delete', () => {
    component.selectHostel(hostel);
    http.expectOne('/api/hostels/h1/expenses').flush([expense]);

    jest.spyOn(window, 'confirm').mockReturnValue(true);
    component.deleteExpense(expense);

    const remove = http.expectOne('/api/hostels/h1/expenses/e1');
    expect(remove.request.method).toBe('DELETE');
    remove.flush(null);

    http.expectOne('/api/hostels/h1/expenses').flush([]);

    expect(component.expenses()).toEqual([]);
    expect(toast.toasts()[0].kind).toBe('success');
  });
});
