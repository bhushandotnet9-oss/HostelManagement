import { Component, OnInit, signal } from '@angular/core';
import { Hostel, HostelService } from '../hostels/hostel.service';
import { ToastService } from '../shared/toast.service';
import { ExpenseCategory, ExpenseCategoryTotal, ExpenseService } from './expense.service';

@Component({
  selector: 'app-expenses',
  standalone: false,
  templateUrl: './expenses.component.html',
})
export class ExpensesComponent implements OnInit {
  readonly hostels = signal<Hostel[]>([]);
  readonly selectedHostel = signal<Hostel | null>(null);
  readonly breakdown = signal<ExpenseCategoryTotal[]>([]);

  newCategory: ExpenseCategory = 'Electricity';
  newAmount = 0;
  newDate = '';
  newNotes = '';

  rangeFrom = '';
  rangeTo = '';

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
        if (this.rangeFrom && this.rangeTo) {
          this.loadBreakdown();
        }
        this.toast.success('Expense recorded successfully.');
      },
      error: () => this.toast.error('Failed to record expense.'),
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
