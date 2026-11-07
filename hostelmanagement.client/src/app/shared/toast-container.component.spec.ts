import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastContainerComponent } from './toast-container.component';
import { ToastService } from './toast.service';

describe('ToastContainerComponent', () => {
  let fixture: ComponentFixture<ToastContainerComponent>;
  let toastService: ToastService;

  beforeAll(() => {
    (globalThis as any).bootstrap = {
      Toast: class {
        constructor(private readonly el: HTMLElement) {}
        show(): void {}
      },
    };
  });

  afterAll(() => {
    delete (globalThis as any).bootstrap;
  });

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
    fixture.detectChanges();

    const toastEl: HTMLElement = fixture.nativeElement.querySelector('.toast');
    expect(toastEl).toBeTruthy();
    expect(toastEl.textContent).toContain('Saved successfully');
  });
});
