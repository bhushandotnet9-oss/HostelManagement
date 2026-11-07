import { HttpClient } from '@angular/common/http';
import { computed, Injectable, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface AuthResponse {
  token: string;
  email: string;
  ownerOrgId: string;
}

const TOKEN_KEY = 'hostelmanagement.token';
const EMAIL_KEY = 'hostelmanagement.email';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly currentEmail = signal<string | null>(localStorage.getItem(EMAIL_KEY));
  readonly isAuthenticated = computed(() => this.currentEmail() !== null);

  constructor(private readonly http: HttpClient) {}

  setup(orgName: string, email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/account/setup', { orgName, email, password })
      .pipe(tap((response) => this.storeSession(response)));
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>('/api/account/login', { email, password })
      .pipe(tap((response) => this.storeSession(response)));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(EMAIL_KEY);
    this.currentEmail.set(null);
  }

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private storeSession(response: AuthResponse): void {
    localStorage.setItem(TOKEN_KEY, response.token);
    localStorage.setItem(EMAIL_KEY, response.email);
    this.currentEmail.set(response.email);
  }
}
