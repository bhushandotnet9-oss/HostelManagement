import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { ToastService } from '../shared/toast.service';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-setup',
  standalone: false,
  templateUrl: './setup.component.html',
})
export class SetupComponent {
  orgName = '';
  email = '';
  password = '';

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
    private readonly toast: ToastService,
  ) {}

  submit(): void {
    this.auth.setup(this.orgName, this.email, this.password).subscribe({
      next: () => {
        this.toast.success('Account created successfully.');
        this.router.navigateByUrl('/dashboard');
      },
      error: (err) => {
        this.toast.error(
          err.status === 409 ? 'An account with this email already exists.' : 'Setup failed. Please check your details.',
        );
      },
    });
  }
}
