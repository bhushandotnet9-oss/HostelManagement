import { Component, signal } from '@angular/core';
import { Router } from '@angular/router';
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
  readonly error = signal<string | null>(null);

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router,
  ) {}

  submit(): void {
    this.error.set(null);
    this.auth.setup(this.orgName, this.email, this.password).subscribe({
      next: () => this.router.navigateByUrl('/dashboard'),
      error: (err) => {
        this.error.set(
          err.status === 409 ? 'An account with this email already exists.' : 'Setup failed. Please check your details.',
        );
      },
    });
  }
}
