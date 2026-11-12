import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Menu } from 'primeng/menu';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { ColumnFilter, SortIcon, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';

import { EmployeeRole } from '@shared/employee/employee';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import type { GridSort } from '@shared/grid/requests/grid-request';
import { ChipSummary } from '@shared/chips/chip-summary/chip-summary';
import { labelChipsOf } from '@shared/chips/chip-summary/chip-summary.utils';
import { createGridTable } from '@shared/grid/table/grid-table';
import { createRowMenu } from '@shared/grid/table/row-menu';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import {
  EmployeeColumn,
  type EmployeeFilter,
  type EmployeeRow,
  type EmployeeSort,
  type EmployeeSortColumn,
  EmployeeStatus,
} from '@employees/employee';
import { isRetryable } from '@employees/list/employees-grid.store.utils';
import {
  ALL_FILTER_READERS,
  ALL_FILTER_WRITERS,
  COLUMN_LABEL_KEYS,
  MENU_KEYS,
  MENU_TOOLTIP_KEYS,
  ROLE_LABEL_KEYS,
  SORTABLE_COLUMNS,
  STATUS_LABEL_KEYS,
} from './employee-grid.utils';

const EMPLOYEE_COLUMNS: readonly EmployeeColumn[] = Object.values(EmployeeColumn);

@Component({
  imports: [
    ChipSummary,
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
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-employee-grid',
  templateUrl: './employee-grid.html',
})
export class EmployeeGrid {
  readonly rows = input.required<readonly EmployeeRow[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<EmployeeSort | null>();
  readonly filter = input.required<EmployeeFilter>();
  readonly canManage = input(false);
  readonly canReadShifts = input(false);

  readonly filterChange = output<Partial<EmployeeFilter>>();
  readonly sortChange = output<GridSort<EmployeeSortColumn> | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly shiftsRequested = output<string>();
  readonly scheduleRequested = output<string>();
  readonly retryRequested = output<string>();

  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  protected readonly EmployeeColumn = EmployeeColumn;
  protected readonly columns = EMPLOYEE_COLUMNS;
  protected readonly pageSizes = [...PAGE_SIZES];

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      ...Object.values(ROLE_LABEL_KEYS),
      ...Object.values(STATUS_LABEL_KEYS),
      ...Object.values(MENU_KEYS),
      ...Object.values(MENU_TOOLTIP_KEYS),
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly grid = createGridTable<EmployeeRow, EmployeeColumn, EmployeeFilter, EmployeeSortColumn>({
    allColumns: EMPLOYEE_COLUMNS,
    columns: signal(EMPLOYEE_COLUMNS),
    filter: this.filter,
    sort: this.sort,
    limit: this.limit,
    isLoading: this.isLoading,
    hasLoaded: this.hasLoaded,
    rows: this.rows,
    filterReaders: ALL_FILTER_READERS,
    filterWriters: ALL_FILTER_WRITERS,
    sortableColumns: SORTABLE_COLUMNS,
    labelKeys: COLUMN_LABEL_KEYS,
    filterChange: (patch) => this.filterChange.emit(patch),
    sortChange: (sort) => this.sortChange.emit(sort),
    pageChange: (page) => this.pageChange.emit(page),
    columnsChange: () => undefined,
    columnsReorder: () => undefined,
  });

  protected readonly rowMenu = createRowMenu<EmployeeRow>();
  protected readonly menuItems = signal<MenuItem[]>([]);
  protected readonly roleOptions = computed(() =>
    Object.values(EmployeeRole).map((value) => ({ value, label: this._labels()[ROLE_LABEL_KEYS[value]] ?? value })),
  );
  protected readonly statusOptions = computed(() =>
    Object.values(EmployeeStatus).map((value) => ({ value, label: this._labels()[STATUS_LABEL_KEYS[value]] ?? value })),
  );
  protected readonly labelChips = labelChipsOf;
  protected readonly tableStyle = computed(() => ({
    'min-width': '56rem',
    ...(this.grid.tableRows().length > 0 ? {} : { height: '100%' }),
  }));

  protected roleLabel(role: EmployeeRole): string {
    return this._labels()[ROLE_LABEL_KEYS[role]] ?? role;
  }

  protected statusLabel(status: EmployeeStatus): string {
    return this._labels()[STATUS_LABEL_KEYS[status]] ?? status;
  }

  protected hasMenu(row: EmployeeRow): boolean {
    return this.canReadShifts() || (this.canManage() && isRetryable(row.status));
  }

  /** Builds the menu of the row at the click: the retry item shows only for a row that can be retried. */
  protected openMenu(event: Event, row: EmployeeRow, menu: Menu): void {
    this.menuItems.set(this._menuItemsFor(row));
    this.rowMenu.open(event, row, menu);
  }

  private _menuItemsFor(row: EmployeeRow): MenuItem[] {
    const labels = this._labels();
    const shiftItems: MenuItem[] = this.canReadShifts()
      ? [
          this.rowMenu.menuItem(
            {
              labelKey: MENU_KEYS.shifts,
              tooltipKey: MENU_TOOLTIP_KEYS.shifts,
              icon: 'pi pi-clock',
              onSelect: (employee) => this.shiftsRequested.emit(employee.id),
            },
            labels,
          ),
          this.rowMenu.menuItem(
            {
              labelKey: MENU_KEYS.schedule,
              tooltipKey: MENU_TOOLTIP_KEYS.schedule,
              icon: 'pi pi-calendar',
              onSelect: (employee) => this.scheduleRequested.emit(employee.id),
            },
            labels,
          ),
        ]
      : [];
    const retryItems: MenuItem[] =
      this.canManage() && isRetryable(row.status)
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_KEYS.retry,
                tooltipKey: MENU_TOOLTIP_KEYS.retry,
                icon: 'pi pi-refresh',
                onSelect: (employee) => this.retryRequested.emit(employee.id),
              },
              labels,
            ),
          ]
        : [];
    return [...shiftItems, ...retryItems];
  }
}
