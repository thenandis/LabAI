import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LoadingService } from './core/http/loading.service';
import { ToastContainer } from './core/notifications/toast-container';

/**
 * The application SHELL: header, global progress bar, toasts, and the router outlet
 * where the lazy-loaded feature pages render.
 *
 * OnPush: Angular re-checks this component only when its inputs or the signals it
 * reads change, not on every event anywhere in the app.
 */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastContainer],
  templateUrl: './app.html',
})
export class App {
  protected readonly loading = inject(LoadingService);
}
