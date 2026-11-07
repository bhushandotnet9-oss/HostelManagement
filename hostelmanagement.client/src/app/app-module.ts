import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { AuthInterceptor } from './auth/auth.interceptor';
import { LoginComponent } from './auth/login.component';
import { SetupComponent } from './auth/setup.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { HostelsComponent } from './hostels/hostels.component';
import { ResidentsComponent } from './residents/residents.component';
import { ExpensesComponent } from './expenses/expenses.component';
import { NavShellComponent } from './shared/nav-shell.component';
import { ToastContainerComponent } from './shared/toast-container.component';

@NgModule({
  declarations: [
    App,
    LoginComponent,
    SetupComponent,
    DashboardComponent,
    HostelsComponent,
    ResidentsComponent,
    ExpensesComponent,
    NavShellComponent,
    ToastContainerComponent,
  ],
  imports: [
    BrowserModule, HttpClientModule,
    FormsModule,
    AppRoutingModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
  ],
  bootstrap: [App]
})
export class AppModule { }
