import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { AbstractControl, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { ProblemDetails, toProblemDetails } from '../../../core/models/problem-details';
import { NotificationService } from '../../../core/notifications/notification.service';
import { LookupApiService } from '../data-access/lookup-api.service';
import { CreateProduct, Product } from '../data-access/product.models';
import { ProductApiService } from '../data-access/product-api.service';
import { ProductStore } from '../data-access/product.store';

const SMALLINT_MAX = 32767; // SQL SMALLINT upper bound (UnitsInStock etc.)

/**
 * The "C" and "U" of CRUD: one component handles both create and edit.
 *
 * Routes: /products/new → create mode; /products/:id/edit → edit mode.
 * `id` arrives as a component INPUT straight from the route parameter, thanks to
 * `withComponentInputBinding()` in app.config.ts (no ActivatedRoute boilerplate).
 *
 * VALIDATION IN TWO LAYERS:
 *  1. Client-side (Validators.*): instant feedback, and saves a round-trip.
 *  2. Server-side (FluentValidation): the REAL authority. Never trust the client.
 *     A 400 response's `errors` dictionary is mapped onto the matching form controls,
 *     so server-only rules ("Category 99 does not exist") appear under the right field.
 */
@Component({
  selector: 'app-product-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './product-form.html',
})
export class ProductForm implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly api = inject(ProductApiService);
  private readonly store = inject(ProductStore);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);
  private readonly lookups = inject(LookupApiService);

  /** Route param (undefined in create mode). */
  readonly id = input<string>();

  protected readonly isEdit = computed(() => !!this.id());
  protected readonly categories = toSignal(this.lookups.categories$, { initialValue: [] });
  protected readonly suppliers = toSignal(this.lookups.suppliers$, { initialValue: [] });

  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly loadError = signal<string | null>(null);
  /** Set on 409: someone else saved this product after we loaded it. */
  protected readonly conflict = signal<ProblemDetails | null>(null);

  /** The concurrency token from the last load/save, sent back on update. */
  private rowVersion = '';
  private saved = false;

  /**
   * TYPED REACTIVE FORM. NonNullableFormBuilder infers each control's type, so
   * `form.getRawValue().unitPrice` is `number`, not `any`. Field names match the API's
   * camelCase JSON exactly, so the value can be sent as-is and server errors map back by key.
   */
  protected readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(40)]],
    categoryId: this.fb.control<number | null>(null),
    supplierId: this.fb.control<number | null>(null),
    quantityPerUnit: ['', [Validators.maxLength(20)]],
    unitPrice: [0, [Validators.required, Validators.min(0), Validators.max(1_000_000)]],
    unitsInStock: [0, [Validators.required, Validators.min(0), Validators.max(SMALLINT_MAX)]],
    unitsOnOrder: [0, [Validators.required, Validators.min(0), Validators.max(SMALLINT_MAX)]],
    reorderLevel: [0, [Validators.required, Validators.min(0), Validators.max(SMALLINT_MAX)]],
    discontinued: [false],
  });

  ngOnInit(): void {
    if (this.isEdit()) this.load();
  }

  /** Used by the route's canDeactivate guard: warn before throwing away unsaved edits. */
  canLeave(): boolean {
    return this.saved || !this.form.dirty || confirm('You have unsaved changes. Leave this page?');
  }

  protected load(): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.conflict.set(null);

    this.api.getById(Number(this.id())).subscribe({
      next: (product) => {
        this.fillForm(product);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        const problem = toProblemDetails(err);
        this.loadError.set(problem.status === 404 ? 'This product no longer exists.' : (problem.title ?? 'Failed to load.'));
        this.loading.set(false);
      },
    });
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched(); // reveal every validation message at once
      return;
    }

    this.saving.set(true);
    this.conflict.set(null);

    const raw = this.form.getRawValue();
    const payload: CreateProduct = {
      ...raw,
      name: raw.name.trim(),
      quantityPerUnit: raw.quantityPerUnit.trim() || null,
      // <select> values can arrive as strings; normalise to number | null
      categoryId: raw.categoryId === null ? null : Number(raw.categoryId),
      supplierId: raw.supplierId === null ? null : Number(raw.supplierId),
    };

    const request$: Observable<Product> = this.isEdit()
      ? this.api.update(Number(this.id()), { ...payload, rowVersion: this.rowVersion })
      : this.api.create(payload);

    request$.subscribe({
      next: (product) => {
        this.saved = true;
        this.saving.set(false);
        this.notifications.success(`"${product.name}" was ${this.isEdit() ? 'updated' : 'created'}.`);
        this.store.reload(); // the list re-fetches, and the API already evicted its output cache
        void this.router.navigate(['/products']);
      },
      error: (err: unknown) => {
        this.saving.set(false);
        this.handleSaveError(toProblemDetails(err));
      },
    });
  }

  /** True when a control should display its error (touched or server-flagged). */
  protected showError(name: keyof typeof this.form.controls): boolean {
    const c = this.form.controls[name];
    return c.invalid && (c.touched || c.dirty || !!c.errors?.['server']);
  }

  /** Turns a control's error object into one readable message. */
  protected errorFor(name: keyof typeof this.form.controls): string {
    const errors = this.form.controls[name].errors;
    if (!errors) return '';
    if (errors['server']) return errors['server'] as string;
    if (errors['required']) return 'This field is required.';
    if (errors['maxlength']) return `Maximum ${errors['maxlength'].requiredLength} characters.`;
    if (errors['min']) return `Must be at least ${errors['min'].min}.`;
    if (errors['max']) return `Must be at most ${errors['max'].max}.`;
    return 'Invalid value.';
  }

  private handleSaveError(problem: ProblemDetails): void {
    switch (problem.status) {
      case 400:
        // Map { errors: { unitPrice: ["..."] } } onto the matching controls.
        this.applyServerErrors(problem.errors ?? {});
        this.notifications.error('Please fix the highlighted fields.');
        break;
      case 404:
        this.loadError.set('This product was deleted by someone else.');
        break;
      case 409:
        // OPTIMISTIC CONCURRENCY conflict: don't overwrite their change. Let the user decide.
        this.conflict.set(problem);
        break;
      // 0 / 429 / 5xx were already toasted by errorInterceptor.
    }
  }

  private applyServerErrors(errors: Record<string, string[]>): void {
    for (const [field, messages] of Object.entries(errors)) {
      const control: AbstractControl | null = this.form.get(field);
      if (control) {
        control.setErrors({ server: messages.join(' ') });
        control.markAsTouched();
      }
    }
  }

  private fillForm(product: Product): void {
    this.rowVersion = product.rowVersion;
    this.form.reset({
      name: product.name,
      categoryId: product.categoryId,
      supplierId: product.supplierId,
      quantityPerUnit: product.quantityPerUnit ?? '',
      unitPrice: product.unitPrice,
      unitsInStock: product.unitsInStock,
      unitsOnOrder: product.unitsOnOrder,
      reorderLevel: product.reorderLevel,
      discontinued: product.discontinued,
    });
  }
}
