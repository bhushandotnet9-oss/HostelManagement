import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { LoginComponent } from './auth/login.component';
import { SetupComponent } from './auth/setup.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { HostelsComponent } from './hostels/hostels.component';
import { ResidentsComponent } from './residents/residents.component';
import { ExpensesComponent } from './expenses/expenses.component';

const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: 'login', component: LoginComponent },
  { path: 'setup', component: SetupComponent },
  { path: 'dashboard', component: DashboardComponent, canActivate: [authGuard] },
  { path: 'hostels', component: HostelsComponent, canActivate: [authGuard] },
  { path: 'residents', component: ResidentsComponent, canActivate: [authGuard] },
  { path: 'expenses', component: ExpensesComponent, canActivate: [authGuard] },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
