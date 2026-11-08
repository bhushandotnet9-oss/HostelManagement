import { Component } from '@angular/core';
import { ToastService } from './toast.service';

/**
 * Renders live toasts directly, rather than handing each element to Bootstrap's
 * Toast widget after render. The widget approach depended on a ViewChildren
 * query being populated in the same change-detection pass that created the
 * element: it held for success toasts (whose handlers trigger further state
 * changes and so further passes) but not for error toasts, which rendered their
 * text and then never received the `.show` class that makes a `.toast` visible.
 * Angular owns the classes here, so visibility no longer depends on that timing.
 * Auto-dismiss lives in ToastService.
 */
@Component({
  selector: 'app-toast-container',
  standalone: false,
  templateUrl: './toast-container.component.html',
})
export class ToastContainerComponent {
  constructor(readonly toastService: ToastService) {}
}
