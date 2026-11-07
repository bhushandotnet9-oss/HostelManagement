import { AfterViewChecked, Component, ElementRef, QueryList, ViewChildren } from '@angular/core';
import { ToastService } from './toast.service';

declare const bootstrap: any;

@Component({
  selector: 'app-toast-container',
  standalone: false,
  templateUrl: './toast-container.component.html',
})
export class ToastContainerComponent implements AfterViewChecked {
  @ViewChildren('toastEl') private readonly toastEls!: QueryList<ElementRef<HTMLElement>>;
  private readonly shown = new Set<number>();

  constructor(readonly toastService: ToastService) {}

  ngAfterViewChecked(): void {
    this.toastEls.forEach((ref) => {
      const el = ref.nativeElement;
      const id = Number(el.dataset['toastId']);
      if (this.shown.has(id)) {
        return;
      }
      this.shown.add(id);

      const toast = new bootstrap.Toast(el, { autohide: true });
      el.addEventListener('hidden.bs.toast', () => {
        this.shown.delete(id);
        this.toastService.dismiss(id);
      });
      toast.show();
    });
  }
}
