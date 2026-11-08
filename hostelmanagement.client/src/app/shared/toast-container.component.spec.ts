import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastContainerComponent } from './toast-container.component';
import { ToastService } from './toast.service';

describe('ToastContainerComponent', () => {
  let fixture: ComponentFixture<ToastContainerComponent>;
  let toastService: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ToastContainerComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ToastContainerComponent);
    toastService = TestBed.inject(ToastService);
  });

  it('renders a toast element for a queued entry', () => {
    toastService.success('Saved successfully');
    fixture.detectChanges();

    const toastEl: HTMLElement = fixture.nativeElement.querySelector('.toast');
    expect(toastEl).toBeTruthy();
    expect(toastEl.textContent).toContain('Saved successfully');
  });

  // Regression: a `.toast` without `.show` is display:none in Bootstrap, so an
  // error toast used to render its text and stay invisible.
  it('marks a success toast shown in a single change-detection pass', () => {
    toastService.success('Saved successfully');
    fixture.detectChanges();

    const toastEl: HTMLElement = fixture.nativeElement.querySelector('.toast');
    expect(toastEl.classList).toContain('show');
    expect(toastEl.classList).toContain('text-bg-success');
  });

  it('marks an error toast shown in a single change-detection pass', () => {
    toastService.error('This hostel still has rooms.');
    fixture.detectChanges();

    const toastEl: HTMLElement = fixture.nativeElement.querySelector('.toast');
    expect(toastEl.classList).toContain('show');
    expect(toastEl.classList).toContain('text-bg-danger');
    expect(toastEl.textContent).toContain('This hostel still has rooms.');
  });

  it('dismisses a toast when its close button is clicked', () => {
    toastService.error('Failed');
    fixture.detectChanges();

    fixture.nativeElement.querySelector('.btn-close').click();
    fixture.detectChanges();

    expect(toastService.toasts()).toEqual([]);
    expect(fixture.nativeElement.querySelector('.toast')).toBeNull();
  });
});
