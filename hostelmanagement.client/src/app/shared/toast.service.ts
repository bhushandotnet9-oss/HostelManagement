import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error';

export interface ToastEntry {
  id: number;
  kind: ToastKind;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  readonly toasts = signal<ToastEntry[]>([]);

  success(message: string): void {
    this.push('success', message);
  }

  error(message: string): void {
    this.push('error', message);
  }

  dismiss(id: number): void {
    this.toasts.update((entries) => entries.filter((entry) => entry.id !== id));
  }

  private push(kind: ToastKind, message: string): void {
    const entry: ToastEntry = { id: this.nextId++, kind, message };
    this.toasts.update((entries) => [...entries, entry]);
  }
}
