import { Component, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Menu } from 'primeng/menu';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { ColumnFilter, SortIcon, Table, type TableFilterEvent, TableModule, type TablePageEvent } from 'primeng/table';

import { EmployeeRole } from '@shared/employee/employee';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { keepWhileLoading, skeletonRows } from '@shared/grid/table/grid-display';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { toSortDirection, toSortOrder } from '@shared/grid/table/grid-sort-order';
import { readTableFilters, toConstraint, toTableFilters } from '@shared/grid/table/table-filters';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import type { EmployeeSortColumn } from '@employees/employee';
import {
  EmployeeColumn,
  type EmployeeFilter,
  type EmployeeRow,
  type EmployeeSort,
  EmployeeStatus,
} from '@employees/employee';
import { isRetryable } from '@employees/list/employees-grid.store.utils';
import {
  COLUMN_LABEL_KEYS,
  FILTER_READERS,
  FILTER_WRITERS,
  FILTERABLE_COLUMNS,
  type FilterableColumn,
  MENU_KEYS,
  ROLE_LABEL_KEYS,
  SORTABLE_COLUMNS,
  STATUS_LABEL_KEYS,
} from './employee-grid.utils';

@Component({
  imports: [
    ButtonDirective,
    ColumnFilter,
    EmptyState,
    FormsModule,
    InputText,
    LocalDateTimePipe,
    Menu,
    Select,
    Skeleton,
    SortIcon,
    TableModule,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-employee-grid',
  templateUrl: './employee-grid.html',
})
export class EmployeeGrid {
  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  readonly rows = input.required<readonly EmployeeRow[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<EmployeeSort | null>();
  readonly filter = input.required<EmployeeFilter>();
  readonly canManage = input(false);
  readonly canReadShifts = input(false);

  readonly filterChange = output<Partial<EmployeeFilter>>();
  readonly sortChange = output<EmployeeSort | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly shiftsRequested = output<string>();
  readonly scheduleRequested = output<string>();
  readonly retryRequested = output<string>();

  protected readonly table = viewChild.required(Table);
  protected readonly EmployeeColumn = EmployeeColumn;
  protected readonly columns = Object.values(EmployeeColumn);
  protected readonly pageSizes = [...PAGE_SIZES];

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      ...Object.values(ROLE_LABEL_KEYS),
      ...Object.values(STATUS_LABEL_KEYS),
      ...Object.values(MENU_KEYS),
    ]),
    { initialValue: {} as Record<string, string> },
  );
  private readonly _activeRow = signal<EmployeeRow | null>(null);

  protected readonly sortOrder = computed(() => toSortOrder(this.sort()?.direction));
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly keptRows = keepWhileLoading(this.rows, this.isLoading);
  protected readonly skeletonRows = computed(() => skeletonRows(this.limit()));
  protected readonly tableFilters = computed(() => toTableFilters(FILTERABLE_COLUMNS, FILTER_WRITERS, this.filter()));
  protected readonly tableRows = computed((): unknown[] =>
    this.showSkeleton() ? this.skeletonRows() : [...this.keptRows()],
  );
  protected readonly tableStyle = computed(() => ({
    'min-width': '56rem',
    ...(this.showSkeleton() || this.keptRows().length > 0 ? {} : { height: '100%' }),
  }));
  protected readonly roleOptions = computed(() =>
    Object.values(EmployeeRole).map((value) => ({ value, label: this._labels()[ROLE_LABEL_KEYS[value]] ?? value })),
  );
  protected readonly statusOptions = computed(() =>
    Object.values(EmployeeStatus).map((value) => ({ value, label: this._labels()[STATUS_LABEL_KEYS[value]] ?? value })),
  );

  protected readonly menuItems = computed<MenuItem[]>(() => {
    const row = this._activeRow();
    const label = (key: string): string => this._labels()[key] ?? key;
    return [
      ...(this.canReadShifts()
        ? [
            {
              label: label(MENU_KEYS.shifts),
              icon: 'pi pi-clock',
              command: (): void => this._emit(this.shiftsRequested),
            },
            {
              label: label(MENU_KEYS.schedule),
              icon: 'pi pi-calendar',
              command: (): void => this._emit(this.scheduleRequested),
            },
          ]
        : []),
      ...(this.canManage() && row !== null && isRetryable(row.status)
        ? [
            {
              label: label(MENU_KEYS.retry),
              icon: 'pi pi-refresh',
              command: (): void => this._emit(this.retryRequested),
            },
          ]
        : []),
    ];
  });

  protected columnLabelKey(column: EmployeeColumn): string {
    return COLUMN_LABEL_KEYS[column];
  }

  protected isSortable(column: EmployeeColumn): boolean {
    return SORTABLE_COLUMNS.includes(column);
  }

  protected isFilterable(column: EmployeeColumn): boolean {
    return (FILTERABLE_COLUMNS as readonly string[]).includes(column);
  }

  protected roleLabel(role: EmployeeRole): string {
    return this._labels()[ROLE_LABEL_KEYS[role]] ?? role;
  }

  protected statusLabel(status: EmployeeStatus): string {
    return this._labels()[STATUS_LABEL_KEYS[status]] ?? status;
  }

  protected onFilterApplied(event: TableFilterEvent): void {
    this.filterChange.emit(readTableFilters(FILTERABLE_COLUMNS, FILTER_READERS, event.filters));
  }

  protected onFilterHidden(column: EmployeeColumn): void {
    const table = this.table();
    table.filters = {
      ...table.filters,
      [column]: [toConstraint(FILTER_WRITERS[column as FilterableColumn](this.filter()))],
    };
  }

  protected onPage(event: TablePageEvent): void {
    this.pageChange.emit({ offset: event.first, limit: event.rows });
  }

  protected onSort(event: { field?: string | null; order?: number | null }): void {
    const column = SORTABLE_COLUMNS.find((candidate) => candidate === event.field) as EmployeeSortColumn | undefined;
    this.sortChange.emit(column === undefined ? null : { column, direction: toSortDirection(event.order) });
  }

  protected openMenu(event: Event, row: EmployeeRow, menu: Menu): void {
    this._activeRow.set(row);
    menu.toggle(event);
  }

  protected hasMenu(row: EmployeeRow): boolean {
    return this.canReadShifts() || (this.canManage() && isRetryable(row.status));
  }

  private _emit(target: { emit: (id: string) => void }): void {
    const row = this._activeRow();
    if (row !== null) {
      target.emit(row.id);
    }
  }
}
