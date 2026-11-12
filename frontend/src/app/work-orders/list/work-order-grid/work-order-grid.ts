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
import { Tag } from 'primeng/tag';
import { ColumnFilter, SortIcon, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';

import { ChipSummary } from '@shared/chips/chip-summary/chip-summary';
import { type Chip, toChips } from '@shared/chips/chip-summary/chip-summary.utils';
import { ColumnChooser } from '@shared/grid/table/column-chooser/column-chooser';
import { createGridTable } from '@shared/grid/table/grid-table';
import { createRowMenu } from '@shared/grid/table/row-menu';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import type { GridSort } from '@shared/grid/requests/grid-request';
import { delayedLoading } from '@shared/loading/delayed-loading';
import type { LookupOption, LookupRequest } from '@shared/lookups/lookup-options';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { PlainDateRangePicker } from '@shared/time/pickers/plain-date-range-picker/plain-date-range-picker';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import {
  WorkOrderCategory,
  WorkOrderColumn,
  type WorkOrderFilter,
  WorkOrderPriority,
  type WorkOrderRow,
  type WorkOrderSort,
  type WorkOrderSortColumn,
  WorkOrderStatus,
} from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import {
  ALL_FILTER_READERS,
  ALL_FILTER_WRITERS,
  CATEGORY_LABEL_KEYS,
  COLUMN_LABEL_KEYS,
  FILTERABLE_COLUMNS,
  MENU_KEYS,
  MENU_TOOLTIP_KEYS,
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

const ALL_COLUMNS: readonly WorkOrderColumn[] = Object.values(WorkOrderColumn);

@Component({
  imports: [
    ButtonDirective,
    ChipSummary,
    ColumnChooser,
    ColumnFilter,
    EmptyState,
    FormsModule,
    InputText,
    LocalDateTimePipe,
    Menu,
    PlainDateRangePicker,
    Select,
    Skeleton,
    SortIcon,
    Tag,
    TableModule,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-work-order-grid',
  templateUrl: './work-order-grid.html',
})
export class WorkOrderGrid {
  readonly rows = input.required<readonly WorkOrderRow[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
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
  protected readonly showEmployeesLoading = delayedLoading(this.isEmployeesLoading);
  protected readonly showMachinesLoading = delayedLoading(this.isMachinesLoading);

  readonly filterChange = output<Partial<WorkOrderFilter>>();
  readonly sortChange = output<GridSort<WorkOrderSortColumn> | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly WorkOrderColumn[]>();
  readonly columnsReorder = output<readonly WorkOrderColumn[]>();
  readonly openRequested = output<string>();
  readonly actionRequested = output<RowActionRequest>();
  readonly employeeLookupRequested = output<LookupRequest>();
  readonly machineLookupRequested = output<LookupRequest>();

  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  protected readonly WorkOrderColumn = WorkOrderColumn;
  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly emptyRange = EMPTY_RANGE;
  protected readonly columnLabelKeys = COLUMN_LABEL_KEYS;

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      ...Object.values(STATUS_LABEL_KEYS),
      ...Object.values(PRIORITY_LABEL_KEYS),
      ...Object.values(CATEGORY_LABEL_KEYS),
      ...Object.values(MENU_KEYS),
      ...Object.values(MENU_TOOLTIP_KEYS),
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly allColumns = computed(() =>
    ALL_COLUMNS.filter((column) => this.canManage() || column !== WorkOrderColumn.Assignee),
  );

  protected readonly grid = createGridTable<WorkOrderRow, WorkOrderColumn, WorkOrderFilter, WorkOrderSortColumn>({
    allColumns: ALL_COLUMNS,
    columns: computed(() => this.columns().filter((column) => this.allColumns().includes(column))),
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
    columnsChange: (columns) => this.columnsChange.emit(columns),
    columnsReorder: (columns) => this.columnsReorder.emit(columns),
  });

  protected readonly tableStyle = computed(() => ({
    'min-width': '72rem',
    ...(this.grid.tableRows().length > 0 ? {} : { height: '100%' }),
  }));

  protected readonly statusOptions = computed(() => this._options(STATUS_LABEL_KEYS, WorkOrderStatus));
  protected readonly priorityOptions = computed(() => this._options(PRIORITY_LABEL_KEYS, WorkOrderPriority));
  protected readonly categoryOptions = computed(() => this._options(CATEGORY_LABEL_KEYS, WorkOrderCategory));

  protected readonly rowMenu = createRowMenu<WorkOrderRow>();
  protected readonly menuItems = signal<MenuItem[]>([]);

  protected statusLabel(status: WorkOrderStatus): string {
    return this._labels()[STATUS_LABEL_KEYS[status]] ?? STATUS_LABEL_KEYS[status];
  }

  protected priorityLabel(priority: WorkOrderPriority): string {
    return this._labels()[PRIORITY_LABEL_KEYS[priority]] ?? PRIORITY_LABEL_KEYS[priority];
  }

  protected statusChips(statuses: readonly WorkOrderStatus[]): Chip[] {
    return toChips(statuses, 'hf-status-', (status) => this.statusLabel(status), Object.values(WorkOrderStatus));
  }

  protected priorityChips(priorities: readonly WorkOrderPriority[]): Chip[] {
    return toChips(
      priorities,
      'hf-priority-',
      (priority) => this.priorityLabel(priority),
      Object.values(WorkOrderPriority),
    );
  }

  protected categoryChips(categories: readonly WorkOrderCategory[]): Chip[] {
    return toChips(categories, '', (category) => this.categoryLabel(category), Object.values(WorkOrderCategory));
  }

  protected assigneeChips(assigneeIds: readonly string[]): Chip[] {
    return toChips(
      assigneeIds,
      '',
      (id) => this.employees().find((option) => option.id === id)?.displayName ?? id,
      this.employees().map((option) => option.id),
    );
  }

  protected machineChips(machineIds: readonly string[]): Chip[] {
    return toChips(
      machineIds,
      '',
      (id) => this.machines().find((option) => option.id === id)?.displayName ?? id,
      this.machines().map((option) => option.id),
    );
  }

  protected categoryLabel(category: WorkOrderCategory): string {
    return this._labels()[CATEGORY_LABEL_KEYS[category]] ?? CATEGORY_LABEL_KEYS[category];
  }

  protected isFilterable(column: WorkOrderColumn): boolean {
    return (FILTERABLE_COLUMNS as readonly WorkOrderColumn[]).includes(column);
  }

  /** Builds the menu of the row at the click: the actions follow the status of the row and the permissions. */
  protected openMenu(event: Event, row: WorkOrderRow, menu: Menu): void {
    this.menuItems.set(this._menuItemsFor(row));
    this.rowMenu.open(event, row, menu);
  }

  private _options<TValue extends string>(
    keys: Readonly<Record<TValue, string>>,
    values: Record<string, TValue>,
  ): { value: TValue; label: string }[] {
    return Object.values(values).map((value) => ({ value, label: this._labels()[keys[value]] ?? keys[value] }));
  }

  private _menuItemsFor(row: WorkOrderRow): MenuItem[] {
    const labels = this._labels();
    const isScheduled = row.status !== WorkOrderStatus.Backlog;
    const action =
      (type: RowActionType) =>
      (target: WorkOrderRow): void =>
        this.actionRequested.emit({ type, row: target });
    return [
      this.rowMenu.menuItem(
        {
          labelKey: MENU_KEYS.open,
          tooltipKey: MENU_TOOLTIP_KEYS.open,
          icon: 'pi pi-eye',
          onSelect: (target) => this.openRequested.emit(target.id),
        },
        labels,
      ),
      ...(this.canManage() && !isScheduled
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_KEYS.assign,
                tooltipKey: MENU_TOOLTIP_KEYS.assign,
                icon: 'pi pi-user-plus',
                onSelect: action(WorkOrderActionType.Assign),
              },
              labels,
            ),
          ]
        : []),
      ...(this.canManage() && isScheduled
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_KEYS.move,
                tooltipKey: MENU_TOOLTIP_KEYS.move,
                icon: 'pi pi-arrows-alt',
                onSelect: action(WorkOrderActionType.Move),
              },
              labels,
            ),
            this.rowMenu.menuItem(
              {
                labelKey: MENU_KEYS.unassign,
                tooltipKey: MENU_TOOLTIP_KEYS.unassign,
                icon: 'pi pi-user-minus',
                onSelect: action(WorkOrderActionType.Unassign),
              },
              labels,
            ),
          ]
        : []),
      ...(this.canWrite() && isScheduled
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_KEYS.status,
                tooltipKey: MENU_TOOLTIP_KEYS.status,
                icon: 'pi pi-flag',
                onSelect: action(WorkOrderActionType.ChangeStatus),
              },
              labels,
            ),
          ]
        : []),
      ...(this.canManage()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_KEYS.delete,
                tooltipKey: MENU_TOOLTIP_KEYS.delete,
                icon: 'pi pi-trash',
                onSelect: action(WorkOrderActionType.Delete),
              },
              labels,
            ),
          ]
        : []),
    ];
  }
}
