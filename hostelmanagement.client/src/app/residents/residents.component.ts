import { Component, OnInit, signal } from '@angular/core';
import { HostelService } from '../hostels/hostel.service';
import { ToastService } from '../shared/toast.service';
import { DuePeriod, DueService } from './due.service';
import { BillingCycle, Resident, ResidentService } from './resident.service';

export interface VacantCotOption {
  cotId: string;
  label: string;
}

@Component({
  selector: 'app-residents',
  standalone: false,
  templateUrl: './residents.component.html',
})
export class ResidentsComponent implements OnInit {
  readonly residents = signal<Resident[]>([]);
  readonly vacantCots = signal<VacantCotOption[]>([]);

  newName = '';
  newContactNumber = '';
  newAadhaarNumber = '';
  newMoveInDate = '';

  assignCotId: Record<string, string> = {};
  assignRentAmount: Record<string, number> = {};
  assignDepositAmount: Record<string, number> = {};
  assignBillingCycle: Record<string, BillingCycle> = {};
  assignMoveInDate: Record<string, string> = {};

  noticeDate: Record<string, string> = {};
  noticePeriodDays: Record<string, number> = {};

  checkoutDepositRefund: Record<string, number> = {};

  readonly duesByResident = signal<Record<string, DuePeriod[]>>({});
  paymentAmount: Record<string, number> = {};
  paymentDate: Record<string, string> = {};

  constructor(
    private readonly residentService: ResidentService,
    private readonly hostelService: HostelService,
    private readonly dueService: DueService,
    private readonly toast: ToastService,
  ) {}

  ngOnInit(): void {
    this.reloadResidents();
    this.reloadVacantCots();
  }

  reloadResidents(): void {
    this.residentService.list().subscribe((residents) => this.residents.set(residents));
  }

  reloadVacantCots(): void {
    this.hostelService.listHostels().subscribe((hostels) => {
      const options: VacantCotOption[] = [];
      let pending = hostels.length;
      if (pending === 0) {
        this.vacantCots.set(options);
        return;
      }
      hostels.forEach((hostel) => {
        this.hostelService.listRooms(hostel.id).subscribe((rooms) => {
          let roomsPending = rooms.length;
          if (roomsPending === 0) {
            pending -= 1;
            if (pending === 0) {
              this.vacantCots.set(options);
            }
            return;
          }
          rooms.forEach((room) => {
            this.hostelService.listCots(room.id).subscribe((cots) => {
              cots
                .filter((cot) => cot.status === 'Vacant')
                .forEach((cot) =>
                  options.push({ cotId: cot.id, label: `${hostel.name} / ${room.name} / ${cot.label}` }),
                );
              roomsPending -= 1;
              if (roomsPending === 0) {
                pending -= 1;
                if (pending === 0) {
                  this.vacantCots.set(options);
                }
              }
            });
          });
        });
      });
    });
  }

  createResident(): void {
    if (!this.newName.trim() || !this.newContactNumber.trim() || !this.newAadhaarNumber.trim() || !this.newMoveInDate) {
      return;
    }
    this.residentService
      .create(this.newName, this.newContactNumber, this.newAadhaarNumber, this.newMoveInDate)
      .subscribe({
        next: () => {
          this.newName = '';
          this.newContactNumber = '';
          this.newAadhaarNumber = '';
          this.newMoveInDate = '';
          this.reloadResidents();
          this.toast.success('Resident added successfully.');
        },
        error: () => this.toast.error('Failed to add resident.'),
      });
  }

  assignCot(resident: Resident): void {
    const cotId = this.assignCotId[resident.id];
    const billingCycle = this.assignBillingCycle[resident.id];
    const moveInDate = this.assignMoveInDate[resident.id];
    if (!cotId || !billingCycle || !moveInDate) {
      return;
    }
    this.residentService
      .assignCot(
        resident.id,
        cotId,
        this.assignRentAmount[resident.id] ?? 0,
        this.assignDepositAmount[resident.id] ?? 0,
        billingCycle,
        moveInDate,
      )
      .subscribe({
        next: () => {
          this.reloadResidents();
          this.reloadVacantCots();
          this.toast.success('Cot assigned successfully.');
        },
        error: () => this.toast.error('Failed to assign cot.'),
      });
  }

  recordNotice(resident: Resident): void {
    const noticeDate = this.noticeDate[resident.id];
    const noticePeriodDays = this.noticePeriodDays[resident.id];
    if (!noticeDate || !noticePeriodDays) {
      return;
    }
    this.residentService.recordNotice(resident.id, noticeDate, noticePeriodDays).subscribe({
      next: () => {
        this.reloadResidents();
        this.toast.success('Notice recorded successfully.');
      },
      error: () => this.toast.error('Failed to record notice.'),
    });
  }

  checkout(resident: Resident): void {
    const depositRefundAmount = this.checkoutDepositRefund[resident.id] ?? 0;
    this.residentService.checkout(resident.id, depositRefundAmount).subscribe({
      next: () => {
        this.reloadResidents();
        this.reloadVacantCots();
        this.toast.success('Checkout completed successfully.');
      },
      error: () => this.toast.error('Failed to complete checkout.'),
    });
  }

  loadDues(resident: Resident): void {
    this.dueService.listForResident(resident.id).subscribe((dues) => {
      this.duesByResident.update((current) => ({ ...current, [resident.id]: dues }));
    });
  }

  logPayment(resident: Resident, duePeriod: DuePeriod): void {
    const amount = this.paymentAmount[duePeriod.id];
    const date = this.paymentDate[duePeriod.id];
    if (!amount || !date) {
      return;
    }
    this.dueService.logPayment(duePeriod.id, amount, date).subscribe({
      next: () => {
        this.paymentAmount[duePeriod.id] = 0;
        this.paymentDate[duePeriod.id] = '';
        this.loadDues(resident);
        this.toast.success('Payment logged successfully.');
      },
      error: () => this.toast.error('Failed to log payment.'),
    });
  }
}
