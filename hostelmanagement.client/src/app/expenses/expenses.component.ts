import { Component, OnInit, signal } from '@angular/core';
import { Hostel, HostelService } from '../hostels/hostel.service';
import { apiErrorMessage } from '../shared/api-error';
import { ToastService } from '../shared/toast.service';
import { Expense, ExpenseCategory, ExpenseCategoryTotal, ExpenseService } from './expense.service';

@Component({
  selector: 'app-expenses',
  standalone: false,
  templateUrl: './expenses.component.html',
})
export class ExpensesComponent implements OnInit {
  readonly hostels = signal<Hostel[]>([]);
  readonly selectedHostel = signal<Hostel | null>(null);
  readonly breakdown = signal<ExpenseCategoryTotal[]>([]);
  readonly expenses = signal<Expense[]>([]);

  newCategory: ExpenseCategory = 'Electricity';
  newAmount = 0;
  newDate = '';
  newNotes = '';

  rangeFrom = '';
  rangeTo = '';

  readonly editingExpenseId = signal<string | null>(null);
  editCategory: ExpenseCategory = 'Electricity';
  editAmount = 0;
  editDate = '';
  editNotes = '';

  constructor(
    private readonly hostelService: HostelService,
    private readonly expenseService: ExpenseService,
    private readonly toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.hostelService.listHostels().subscribe((hostels) => this.hostels.set(hostels));
  }

  selectHostel(hostel: Hostel): void {
    this.selectedHostel.set(hostel);
    this.breakdown.set([]);
    this.editingExpenseId.set(null);
    this.loadExpenses();
  }

  loadExpenses(): void {
    const hostel = this.selectedHostel();
    if (!hostel) {
      return;
    }
    this.expenseService.list(hostel.id).subscribe((expenses) => this.expenses.set(expenses));
  }

  createExpense(): void {
    const hostel = this.selectedHostel();
    if (!hostel || this.newAmount <= 0 || !this.newDate) {
      return;
    }
    this.expenseService.create(hostel.id, this.newCategory, this.newAmount, this.newDate, this.newNotes).subscribe({
      next: () => {
        this.newAmount = 0;
        this.newDate = '';
        this.newNotes = '';
        this.loadExpenses();
        if (this.rangeFrom && this.rangeTo) {
          this.loadBreakdown();
        }
        this.toast.success('Expense recorded successfully.');
      },
      error: () => this.toast.error('Failed to record expense.'),
    });
  }

  startEditExpense(expense: Expense): void {
    this.editingExpenseId.set(expense.id);
    this.editCategory = expense.category;
    this.editAmount = expense.amount;
    this.editDate = expense.date;
    this.editNotes = expense.notes ?? '';
  }

  cancelEditExpense(): void {
    this.editingExpenseId.set(null);
  }

  saveExpense(expense: Expense): void {
    const hostel = this.selectedHostel();
    if (!hostel || this.editAmount <= 0 || !this.editDate) {
      return;
    }
    this.expenseService
      .update(hostel.id, expense.id, this.editCategory, this.editAmount, this.editDate, this.editNotes)
      .subscribe({
        next: () => {
          this.editingExpenseId.set(null);
          this.loadExpenses();
          if (this.rangeFrom && this.rangeTo) {
            this.loadBreakdown();
          }
          this.toast.success('Expense updated successfully.');
        },
        error: (error) => this.toast.error(apiErrorMessage(error, 'Failed to update expense.')),
      });
  }

  deleteExpense(expense: Expense): void {
    const hostel = this.selectedHostel();
    if (!hostel || !confirm('Delete this expense? This cannot be undone.')) {
      return;
    }
    this.expenseService.remove(hostel.id, expense.id).subscribe({
      next: () => {
        this.loadExpenses();
        if (this.rangeFrom && this.rangeTo) {
          this.loadBreakdown();
        }
        this.toast.success('Expense deleted successfully.');
      },
      error: (error) => this.toast.error(apiErrorMessage(error, 'Failed to delete expense.')),
    });
  }

  loadBreakdown(): void {
    const hostel = this.selectedHostel();
    if (!hostel || !this.rangeFrom || !this.rangeTo) {
      return;
    }
    this.expenseService.breakdown(hostel.id, this.rangeFrom, this.rangeTo).subscribe((totals) => this.breakdown.set(totals));
  }
}
