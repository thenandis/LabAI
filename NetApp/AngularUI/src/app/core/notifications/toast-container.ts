import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { NotificationService } from './notification.service';

/**
 * Renders the toast stack. Placed once in the app shell (app.html).
 *
 * Accessibility: `aria-live="polite"` makes screen readers announce new toasts
 * without interrupting the user; errors use role="alert", so they're announced immediately.
 */
@Component({
  selector: 'app-toast-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toast-stack" aria-live="polite">
      @for (toast of notifications.toasts(); track toast.id) {
        <div class="toast toast--{{ toast.kind }}" [attr.role]="toast.kind === 'error' ? 'alert' : 'status'">
          <span>{{ toast.message }}</span>
          <button type="button" class="toast__close" (click)="notifications.dismiss(toast.id)" aria-label="Dismiss">×</button>
        </div>
      }
    </div>
  `,
})
export class ToastContainer {
  protected readonly notifications = inject(NotificationService);
}
