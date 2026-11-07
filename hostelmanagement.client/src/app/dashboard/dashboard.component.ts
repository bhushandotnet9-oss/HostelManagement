import { Component, OnInit, signal } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { Hostel, HostelService, RevenueSummary } from '../hostels/hostel.service';
import {
  DashboardService,
  HostelExpenseTotals,
  OccupancySummary,
  OverdueResident,
  UpcomingDue,
} from './dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  readonly email;

  readonly overdue = signal<OverdueResident[]>([]);
  readonly upcomingDues = signal<UpcomingDue[]>([]);
  readonly occupancy = signal<OccupancySummary | null>(null);
  readonly expenseTotals = signal<HostelExpenseTotals[]>([]);
  readonly revenueByHostel = signal<Record<string, RevenueSummary>>({});

  constructor(
    private readonly auth: AuthService,
    private readonly dashboardService: DashboardService,
    private readonly hostelService: HostelService,
  ) {
    this.email = this.auth.currentEmail;
  }

  ngOnInit(): void {
    this.dashboardService.overdue().subscribe((overdue) => this.overdue.set(overdue));
    this.dashboardService.upcomingDues(7).subscribe((upcoming) => this.upcomingDues.set(upcoming));
    this.dashboardService.occupancy().subscribe((occupancy) => this.occupancy.set(occupancy));

    const { from, to } = this.currentMonthRange();
    this.dashboardService.expenseTotals(from, to).subscribe((totals) => this.expenseTotals.set(totals));

    this.hostelService.listHostels().subscribe((hostels) => {
      hostels.forEach((hostel: Hostel) => {
        this.hostelService.revenue(hostel.id, from, to).subscribe((revenue) => {
          this.revenueByHostel.update((current) => ({ ...current, [hostel.id]: revenue }));
        });
      });
    });
  }

  private currentMonthRange(): { from: string; to: string } {
    const now = new Date();
    const from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().slice(0, 10);
    const to = now.toISOString().slice(0, 10);
    return { from, to };
  }

}
