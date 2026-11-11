import { ChangeDetectionStrategy, Component, ElementRef, computed, inject, signal, viewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ListService, LocalizationPipe, LocalizationService } from '@abp/ng.core';
import { Confirmation, ConfirmationService, ToasterService } from '@abp/ng.theme.shared';
import { EMPTY, catchError, finalize } from 'rxjs';
import { PantryItemDto, PantryItemService, PantryItemStatus } from '../proxy/pantry-items';

type SortField = 'ExpirationDate' | 'ProductName' | 'Quantity' | 'Unit';

// Espeja PantryItemConsts.MaxUnitLength del backend; el servidor sigue siendo quien valida.
const UNIT_MAX_LENGTH = 20;
const MIN_EXPIRATION_YEAR = 1900;
const MAX_EXPIRATION_YEAR = 2100;

// Fecha vacía = sin fecha. Si hay valor, debe ser una fecha real AAAA-MM-DD con un año razonable.
function expirationDateValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value as string | null;
  if (!value) {
    return null;
  }

  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) {
    return { invalidDate: true };
  }

  const year = Number(match[1]);
  const month = Number(match[2]);
  const day = Number(match[3]);
  const date = new Date(year, month - 1, day);
  const isRealDate =
    date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day;
  const inRange = year >= MIN_EXPIRATION_YEAR && year <= MAX_EXPIRATION_YEAR;

  return isRealDate && inRange ? null : { invalidDate: true };
}

@Component({
  selector: 'app-pantry',
  templateUrl: './pantry.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, LocalizationPipe, ReactiveFormsModule],
  providers: [ListService],
})
export class PantryComponent {
  private readonly pantryItemService = inject(PantryItemService);
  private readonly list = inject(ListService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly toaster = inject(ToasterService);
  private readonly localization = inject(LocalizationService);
  private readonly fb = inject(FormBuilder);

  readonly pageSize = 10;
  readonly unitMaxLength = UNIT_MAX_LENGTH;
  readonly PantryItemStatus = PantryItemStatus;

  readonly items = signal<PantryItemDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly sortField = signal<SortField>('ExpirationDate');
  readonly sortDescending = signal(false);
  readonly includeConsumed = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);

  readonly editing = signal<PantryItemDto | null>(null);
  readonly saving = signal(false);
  readonly busyId = signal<string | null>(null);
  // El campo de fecha nativo entrega texto vacío mientras la fecha está incompleta.
  readonly dateIncomplete = signal(false);

  private readonly expirationDateInput = viewChild<ElementRef<HTMLInputElement>>('expirationDateInput');

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  readonly form = this.fb.group({
    expirationDate: this.fb.control('', [expirationDateValidator]),
    quantity: this.fb.control<number | null>(null, [Validators.min(0)]),
    unit: this.fb.control('', [Validators.maxLength(UNIT_MAX_LENGTH)]),
  });

  constructor() {
    // ListService arma la consulta (página, orden y cantidad); el componente aporta el resto.
    this.list.maxResultCount = this.pageSize;
    this.list.sortKey = this.sortField();
    this.list.sortOrder = 'asc';

    this.list
      .hookToQuery(query => this.fetch(query))
      .pipe(takeUntilDestroyed())
      .subscribe(result => {
        this.items.set(result.items ?? []);
        this.totalCount.set(result.totalCount ?? 0);
        this.loading.set(false);
      });
  }

  // Vuelve a ejecutar la consulta actual (se usa para reintentar y después de cada acción).
  load(): void {
    this.list.get();
  }

  sortBy(field: SortField): void {
    const descending = this.sortField() === field ? !this.sortDescending() : false;
    this.sortField.set(field);
    this.sortDescending.set(descending);
    this.page.set(1);

    // Cada asignación pide una consulta, pero ListService espera y se queda con la última.
    this.list.page = 0;
    this.list.sortKey = field;
    this.list.sortOrder = descending ? 'desc' : 'asc';
  }

  onIncludeConsumedChange(event: Event): void {
    this.includeConsumed.set((event.target as HTMLInputElement).checked);
    this.page.set(1);

    if (this.list.page !== 0) {
      this.list.page = 0;
    } else {
      this.list.get();
    }
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }
    this.page.set(page);
    this.list.page = page - 1;
  }

  // ---- Editar ----

  startEdit(item: PantryItemDto): void {
    this.editing.set(item);
    this.dateIncomplete.set(false);
    this.form.reset({
      expirationDate: item.expirationDate ?? '',
      quantity: item.quantity ?? null,
      unit: item.unit ?? '',
    });
  }

  cancelEdit(): void {
    this.editing.set(null);
    this.dateIncomplete.set(false);
  }

  onExpirationDateInput(event: Event): void {
    this.dateIncomplete.set((event.target as HTMLInputElement).validity.badInput);
  }

  save(): void {
    const item = this.editing();
    const id = item?.id;
    if (!item || !id || this.saving()) {
      return;
    }

    // El campo date nativo entrega '' mientras la fecha está incompleta y no emite ningún evento
    // al tipear solo una parte: se consulta su validez justo antes de guardar.
    if (this.expirationDateInput()?.nativeElement.validity.badInput) {
      this.dateIncomplete.set(true);
    }

    if (this.form.invalid || this.dateIncomplete()) {
      return;
    }

    const value = this.form.getRawValue();
    this.saving.set(true);
    this.pantryItemService
      .update(
        id,
        {
          expirationDate: value.expirationDate || undefined,
          quantity: value.quantity ?? undefined,
          unit: value.unit?.trim() || undefined,
        },
        { skipHandleError: true },
      )
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.editing.set(null);
          this.toaster.success('::Pantry:Updated');
          this.load();
        },
        error: () => this.toaster.error('::Pantry:ActionError'),
      });
  }

  // ---- Marcar como consumido ----

  markAsConsumed(item: PantryItemDto): void {
    const id = item.id;
    if (!id || this.busyId()) {
      return;
    }

    this.busyId.set(id);
    this.pantryItemService
      .markAsConsumed(id, { skipHandleError: true })
      .pipe(finalize(() => this.busyId.set(null)))
      .subscribe({
        next: () => {
          this.closeEditorIfEditing(item);
          this.toaster.success('::Pantry:ConsumedMessage');
          this.reload(!this.includeConsumed());
        },
        error: () => {
          this.toaster.error('::Pantry:ActionError');
          this.load();
        },
      });
  }

  // ---- Eliminar ----

  deleteItem(item: PantryItemDto): void {
    const id = item.id;
    if (!id || this.busyId()) {
      return;
    }

    const name = item.productName ?? this.localization.instant('::Pantry:NoData');
    this.confirmation
      .warn('::Pantry:DeleteConfirmation', '::Pantry:DeleteTitle', {
        messageLocalizationParams: [name],
      })
      .subscribe(status => {
        if (status !== Confirmation.Status.confirm) {
          return;
        }

        this.busyId.set(id);
        this.pantryItemService
          .delete(id, { skipHandleError: true })
          .pipe(finalize(() => this.busyId.set(null)))
          .subscribe({
            next: () => {
              this.closeEditorIfEditing(item);
              this.toaster.success('::Pantry:Deleted');
              this.reload(true);
            },
            error: () => {
              this.toaster.error('::Pantry:ActionError');
              this.load();
            },
          });
      });
  }

  // ---- Auxiliares ----

  statusKey(item: PantryItemDto): string {
    return item.status === PantryItemStatus.Consumed
      ? '::Pantry:Status:Consumed'
      : '::Pantry:Status:Active';
  }

  sortIndicator(field: SortField): string {
    if (this.sortField() !== field) {
      return '';
    }
    return this.sortDescending() ? '▼' : '▲';
  }

  ariaSort(field: SortField): 'ascending' | 'descending' | 'none' {
    if (this.sortField() !== field) {
      return 'none';
    }
    return this.sortDescending() ? 'descending' : 'ascending';
  }

  private fetch(query: { skipCount: number; maxResultCount: number; sorting?: string }) {
    this.loading.set(true);
    this.failed.set(false);

    return this.pantryItemService
      .getList(
        {
          includeConsumed: this.includeConsumed(),
          sorting: query.sorting,
          skipCount: query.skipCount,
          maxResultCount: query.maxResultCount,
        },
        // El error se informa en la pantalla (con Reintentar): se evita el cuadro predeterminado de ABP.
        { skipHandleError: true },
      )
      .pipe(
        // El error se informa en la pantalla (con Reintentar) y no corta el flujo de ListService.
        catchError(() => {
          this.failed.set(true);
          this.loading.set(false);
          return EMPTY;
        }),
      );
  }

  private closeEditorIfEditing(item: PantryItemDto): void {
    if (this.editing()?.id === item.id) {
      this.editing.set(null);
    }
  }

  // Si se quita el último ítem de una página que no es la primera, vuelve a la anterior.
  private reload(stepBackIfLastOnPage: boolean): void {
    if (stepBackIfLastOnPage && this.items().length === 1 && this.page() > 1) {
      this.goToPage(this.page() - 1);
      return;
    }
    this.load();
  }
}
