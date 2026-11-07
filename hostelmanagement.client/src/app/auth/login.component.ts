import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { ToastService } from '../shared/toast.service';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-login',
  standalone: false,
  templateUrl: './login.component.html',
})
export class LoginComponent {
  email = '';
  password = '';

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly toast: ToastService,
  ) {}

  submit(): void {
    this.auth.login(this.email, this.password).subscribe({
      next: () => {
        this.toast.success('Logged in successfully.');
        this.router.navigateByUrl('/dashboard');
      },
      error: () => this.toast.error('Invalid email or password.'),
    });
  }
}
