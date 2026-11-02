import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { LocalizationPipe } from '@abp/ng.core';
import { Subject, catchError, of, switchMap, tap } from 'rxjs';
import { PantryItemDto, PantryItemService, PantryItemStatus } from '../proxy/pantry-items';

type SortField = 'ExpirationDate' | 'ProductName' | 'Quantity' | 'Unit';

@Component({
  selector: 'app-pantry',
  templateUrl: './pantry.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, LocalizationPipe],
})
export class PantryComponent {
  private readonly pantryItemService = inject(PantryItemService);
  private readonly request$ = new Subject<void>();

  readonly pageSize = 10;
  readonly PantryItemStatus = PantryItemStatus;

  readonly items = signal<PantryItemDto[]>([]);
  readonly totalCount = signal(0);
  readonly page = signal(1);
  readonly sortField = signal<SortField>('ExpirationDate');
  readonly sortDescending = signal(false);
  readonly includeConsumed = signal(false);
  readonly loading = signal(true);
  readonly failed = signal(false);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));

  constructor() {
    this.request$
      .pipe(
        tap(() => {
          this.loading.set(true);
          this.failed.set(false);
        }),
        switchMap(() =>
          this.pantryItemService
            .getList({
              includeConsumed: this.includeConsumed(),
              sorting: `${this.sortField()} ${this.sortDescending() ? 'desc' : 'asc'}`,
              skipCount: (this.page() - 1) * this.pageSize,
              maxResultCount: this.pageSize,
            })
            .pipe(catchError(() => of(null))),
        ),
        takeUntilDestroyed(),
      )
      .subscribe(result => {
        this.loading.set(false);
        if (!result) {
          this.failed.set(true);
          return;
        }
        this.items.set(result.items ?? []);
        this.totalCount.set(result.totalCount ?? 0);
      });

    this.load();
  }

  load(): void {
    this.request$.next();
  }

  sortBy(field: SortField): void {
    if (this.sortField() === field) {
      this.sortDescending.update(descending => !descending);
    } else {
      this.sortField.set(field);
      this.sortDescending.set(false);
    }
    this.page.set(1);
    this.load();
  }

  onIncludeConsumedChange(event: Event): void {
    this.includeConsumed.set((event.target as HTMLInputElement).checked);
    this.page.set(1);
    this.load();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) {
      return;
    }
    this.page.set(page);
    this.load();
  }

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
}
