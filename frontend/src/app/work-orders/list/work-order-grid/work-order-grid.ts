import { Component, computed, inject, input, output, signal, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { Menu } from 'primeng/menu';
import { Popover } from 'primeng/popover';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import {
  ColumnFilter,
  SortIcon,
  Table,
  type TableColumnReorderEvent,
  type TableFilterEvent,
  TableModule,
  type TablePageEvent,
} from 'primeng/table';

import { ChipSummary } from '@shared/chips/chip-summary/chip-summary';
import { type Chip, toChips } from '@shared/chips/chip-summary/chip-summary.utils';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { keepWhileLoading, skeletonRows } from '@shared/grid/table/grid-display';
import type { LookupOption, LookupRequest } from '@shared/lookups/lookup-options';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { toggleColumn } from '@shared/grid/table/grid-columns';
import { toSortDirection, toSortOrder } from '@shared/grid/table/grid-sort-order';
import { readTableFilters, toConstraint, toTableFilters } from '@shared/grid/table/table-filters';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { PlainDateRangePicker } from '@shared/time/pickers/plain-date-range-picker/plain-date-range-picker';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import type { WorkOrderSortColumn } from '@work-orders/work-order';
import {
  WorkOrderCategory,
  WorkOrderColumn,
  type WorkOrderFilter,
  WorkOrderPriority,
  type WorkOrderRow,
  type WorkOrderSort,
  WorkOrderStatus,
} from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import {
  CATEGORY_LABEL_KEYS,
  COLUMN_LABEL_KEYS,
  FILTER_READERS,
  FILTER_WRITERS,
  FILTERABLE_COLUMNS,
  type FilterableColumn,
  MENU_KEYS,
  PRIORITY_LABEL_KEYS,
  SORTABLE_COLUMNS,
  STATUS_LABEL_KEYS,
} from './work-order-grid.utils';

export type RowActionType =
  | typeof WorkOrderActionType.Assign
  | typeof WorkOrderActionType.Move
  | typeof WorkOrderActionType.Unassign
  | typeof WorkOrderActionType.ChangeStatus
  | typeof WorkOrderActionType.Delete;

export interface RowActionRequest {
  readonly type: RowActionType;
  readonly row: WorkOrderRow;
}

@Component({
  imports: [
    ButtonDirective,
    Checkbox,
    ColumnFilter,
    EmptyState,
    FormsModule,
    InputText,
    LocalDateTimePipe,
    Menu,
    PlainDateRangePicker,
    Popover,
    Select,
    Skeleton,
    SortIcon,
    TableModule,
    ChipSummary,
    Tag,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-work-order-grid',
  templateUrl: './work-order-grid.html',
})
export class WorkOrderGrid {
  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  readonly rows = input.required<readonly WorkOrderRow[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly columns = input.required<readonly WorkOrderColumn[]>();
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<WorkOrderSort | null>();
  readonly filter = input.required<WorkOrderFilter>();
  readonly canWrite = input(false);
  readonly canManage = input(false);
  readonly employees = input<LookupOption[]>([]);
  readonly machines = input<LookupOption[]>([]);
  readonly isEmployeesLoading = input(false);
  readonly isMachinesLoading = input(false);

  readonly filterChange = output<Partial<WorkOrderFilter>>();
  readonly sortChange = output<WorkOrderSort | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly WorkOrderColumn[]>();
  readonly columnsReorder = output<readonly WorkOrderColumn[]>();
  readonly openRequested = output<string>();
  readonly actionRequested = output<RowActionRequest>();
  readonly employeeLookupRequested = output<LookupRequest>();
  readonly machineLookupRequested = output<LookupRequest>();

  protected readonly table = viewChild.required(Table);
  protected readonly WorkOrderColumn = WorkOrderColumn;
  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly emptyRange = EMPTY_RANGE;

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      ...Object.values(STATUS_LABEL_KEYS),
      ...Object.values(PRIORITY_LABEL_KEYS),
      ...Object.values(CATEGORY_LABEL_KEYS),
      ...Object.values(MENU_KEYS),
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly allColumns = computed(() =>
    Object.values(WorkOrderColumn).filter((column) => this.canManage() || column !== WorkOrderColumn.Assignee),
  );
  protected readonly tableColumns = computed(() =>
    this.columns().filter((column) => this.canManage() || column !== WorkOrderColumn.Assignee),
  );
  protected readonly sortOrder = computed(() => toSortOrder(this.sort()?.direction));
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly keptRows = keepWhileLoading(this.rows, this.isLoading);
  protected readonly skeletonRows = computed(() => skeletonRows(this.limit()));
  protected readonly tableFilters = computed(() => toTableFilters(FILTERABLE_COLUMNS, FILTER_WRITERS, this.filter()));
  protected readonly tableRows = computed((): unknown[] =>
    this.showSkeleton() ? this.skeletonRows() : [...this.keptRows()],
  );
  protected readonly tableStyle = computed(() => ({
    'min-width': '72rem',
    ...(this.showSkeleton() || this.keptRows().length > 0 ? {} : { height: '100%' }),
  }));

  protected readonly statusOptions = computed(() => this._options(STATUS_LABEL_KEYS, WorkOrderStatus));
  protected readonly priorityOptions = computed(() => this._options(PRIORITY_LABEL_KEYS, WorkOrderPriority));
  protected readonly categoryOptions = computed(() => this._options(CATEGORY_LABEL_KEYS, WorkOrderCategory));

  private readonly _activeRow = signal<WorkOrderRow | null>(null);

  protected readonly menuItems = computed<MenuItem[]>(() => {
    const row = this._activeRow();
    const isScheduled = row !== null && row.status !== WorkOrderStatus.Backlog;
    return [
      this._item(MENU_KEYS.open, 'pi pi-eye', () => this._open()),
      ...(this.canManage() && row?.status === WorkOrderStatus.Backlog
        ? [this._item(MENU_KEYS.assign, 'pi pi-user-plus', () => this._emit(WorkOrderActionType.Assign))]
        : []),
      ...(this.canManage() && isScheduled
        ? [
            this._item(MENU_KEYS.move, 'pi pi-arrows-alt', () => this._emit(WorkOrderActionType.Move)),
            this._item(MENU_KEYS.unassign, 'pi pi-user-minus', () => this._emit(WorkOrderActionType.Unassign)),
          ]
        : []),
      ...(this.canWrite() && isScheduled
        ? [this._item(MENU_KEYS.status, 'pi pi-flag', () => this._emit(WorkOrderActionType.ChangeStatus))]
        : []),
      ...(this.canManage()
        ? [this._item(MENU_KEYS.delete, 'pi pi-trash', () => this._emit(WorkOrderActionType.Delete))]
        : []),
    ];
  });

  protected columnLabelKey(column: WorkOrderColumn): string {
    return COLUMN_LABEL_KEYS[column];
  }

  protected isSortable(column: WorkOrderColumn): boolean {
    return SORTABLE_COLUMNS.includes(column);
  }

  protected isFilterable(column: WorkOrderColumn): boolean {
    return (FILTERABLE_COLUMNS as readonly string[]).includes(column);
  }

  protected statusLabel(status: WorkOrderStatus): string {
    return this._labels()[STATUS_LABEL_KEYS[status]] ?? STATUS_LABEL_KEYS[status];
  }

  protected priorityLabel(priority: WorkOrderPriority): string {
    return this._labels()[PRIORITY_LABEL_KEYS[priority]] ?? PRIORITY_LABEL_KEYS[priority];
  }

  protected statusChips(statuses: readonly WorkOrderStatus[]): Chip[] {
    return toChips(statuses, 'hf-status-', (status) => this.statusLabel(status));
  }

  protected priorityChips(priorities: readonly WorkOrderPriority[]): Chip[] {
    return toChips(priorities, 'hf-priority-', (priority) => this.priorityLabel(priority));
  }

  protected categoryLabel(category: WorkOrderCategory): string {
    return this._labels()[CATEGORY_LABEL_KEYS[category]] ?? CATEGORY_LABEL_KEYS[category];
  }

  protected isColumnVisible(column: WorkOrderColumn): boolean {
    return this.columns().includes(column);
  }

  protected onColumnToggle(column: WorkOrderColumn, isVisible: boolean): void {
    this.columnsChange.emit(toggleColumn(this.columns(), column, isVisible));
  }

  protected onFilterApplied(event: TableFilterEvent): void {
    this.filterChange.emit(readTableFilters(FILTERABLE_COLUMNS, FILTER_READERS, event.filters));
  }

  protected onFilterHidden(column: FilterableColumn): void {
    const table = this.table();
    table.filters = { ...table.filters, [column]: [toConstraint(FILTER_WRITERS[column](this.filter()))] };
  }

  protected onPage(event: TablePageEvent): void {
    this.pageChange.emit({ offset: event.first, limit: event.rows });
  }

  protected onSort(event: { field?: string | null; order?: number | null }): void {
    const column = SORTABLE_COLUMNS.find((candidate) => candidate === event.field) as WorkOrderSortColumn | undefined;
    this.sortChange.emit(column === undefined ? null : { column, direction: toSortDirection(event.order) });
  }

  protected onColumnReorder(event: TableColumnReorderEvent): void {
    this.columnsReorder.emit((event.columns ?? []) as WorkOrderColumn[]);
  }

  protected openMenu(event: Event, row: WorkOrderRow, menu: Menu): void {
    this._activeRow.set(row);
    menu.toggle(event);
  }

  private _options<TValue extends string>(
    keys: Readonly<Record<TValue, string>>,
    values: Record<string, TValue>,
  ): { value: TValue; label: string }[] {
    return Object.values(values).map((value) => ({ value, label: this._labels()[keys[value]] ?? keys[value] }));
  }

  private _item(key: string, icon: string, command: () => void): MenuItem {
    return { label: this._labels()[key] ?? key, icon, command };
  }

  private _open(): void {
    const row = this._activeRow();
    if (row !== null) {
      this.openRequested.emit(row.id);
    }
  }

  private _emit(type: RowActionType): void {
    const row = this._activeRow();
    if (row !== null) {
      this.actionRequested.emit({ type, row });
    }
  }
}
