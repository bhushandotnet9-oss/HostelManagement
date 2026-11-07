import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  it('adds a success entry with the right kind and message', () => {
    service.success('Saved');

    const entries = service.toasts();
    expect(entries.length).toBe(1);
    expect(entries[0].kind).toBe('success');
    expect(entries[0].message).toBe('Saved');
  });

  it('adds an error entry with the right kind and message', () => {
    service.error('Something failed');

    const entries = service.toasts();
    expect(entries.length).toBe(1);
    expect(entries[0].kind).toBe('error');
    expect(entries[0].message).toBe('Something failed');
  });

  it('removes an entry on dismiss', () => {
    service.success('Saved');
    const [entry] = service.toasts();

    service.dismiss(entry.id);

    expect(service.toasts().length).toBe(0);
  });
});
