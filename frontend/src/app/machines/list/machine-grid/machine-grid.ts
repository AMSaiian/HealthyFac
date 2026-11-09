import { Component, computed, inject, input, output, viewChild } from '@angular/core';
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
import {
  ColumnFilter,
  SortIcon,
  Table,
  type TableColumnReorderEvent,
  type TableFilterEvent,
  TableModule,
  type TablePageEvent,
} from 'primeng/table';

import { skeletonRows } from '@shared/grid/table/grid-display';
import { toggleColumn } from '@shared/grid/table/grid-columns';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { toSortDirection, toSortOrder } from '@shared/grid/table/grid-sort-order';
import { readTableFilters, toConstraint, toTableFilters } from '@shared/grid/table/table-filters';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';
import { PlainDateRangePicker } from '@shared/time/pickers/plain-date-range-picker/plain-date-range-picker';

import type { MachineSortColumn } from '@machines/machine';
import { type Machine, MachineColumn, type MachineFilter, type MachineSort, MachineState } from '@machines/machine';
import {
  COLUMN_LABEL_KEYS,
  FILTER_READERS,
  FILTER_WRITERS,
  MENU_DELETE_KEY,
  MENU_FILES_KEY,
  MENU_METRICS_KEY,
  MENU_STATUSES_KEY,
  MENU_UPDATE_KEY,
  SORTABLE_COLUMNS,
  STATE_LABEL_KEYS,
} from './machine-grid.utils';

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
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-machine-grid',
  templateUrl: './machine-grid.html',
})
export class MachineGrid {
  readonly rows = input.required<readonly Machine[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly columns = input.required<readonly MachineColumn[]>();
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<MachineSort | null>();
  readonly filter = input.required<MachineFilter>();
  readonly canWrite = input(false);
  readonly canDelete = input(false);
  readonly canReadStatuses = input(false);
  readonly canReadFiles = input(false);
  readonly canReadMetrics = input(false);

  readonly filterChange = output<Partial<MachineFilter>>();
  readonly sortChange = output<MachineSort | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly MachineColumn[]>();
  readonly columnsReorder = output<readonly MachineColumn[]>();
  readonly updateRequested = output<string>();
  readonly deleteRequested = output<Machine>();
  readonly statusesRequested = output<string>();
  readonly filesRequested = output<string>();
  readonly metricsRequested = output<string>();

  protected readonly table = viewChild.required(Table);
  protected readonly MachineColumn = MachineColumn;
  protected readonly allColumns = Object.values(MachineColumn);
  protected readonly pageSizes = [...PAGE_SIZES];

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      ...Object.values(STATE_LABEL_KEYS),
      MENU_METRICS_KEY,
      MENU_STATUSES_KEY,
      MENU_FILES_KEY,
      MENU_UPDATE_KEY,
      MENU_DELETE_KEY,
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly stateOptions = computed(() =>
    Object.values(MachineState).map((state) => ({
      value: state,
      label: this._labels()[STATE_LABEL_KEYS[state]] ?? STATE_LABEL_KEYS[state],
    })),
  );
  protected readonly sortOrder = computed(() => toSortOrder(this.sort()?.direction));
  protected readonly skeletonRows = computed(() => skeletonRows(this.limit()));
  protected readonly emptyRange = EMPTY_RANGE;
  protected readonly tableFilters = computed(() => toTableFilters(this.allColumns, FILTER_WRITERS, this.filter()));
  protected readonly tableRows = computed((): unknown[] => (this.isLoading() ? this.skeletonRows() : [...this.rows()]));
  protected readonly tableStyle = computed(() => ({
    'min-width': '64rem',
    ...(this.isLoading() || this.rows().length > 0 ? {} : { height: '100%' }),
  }));
  protected readonly tableColumns = computed(() => [...this.columns()]);
  protected readonly hasRowActions = computed(
    () => this.canWrite() || this.canDelete() || this.canReadStatuses() || this.canReadFiles() || this.canReadMetrics(),
  );

  protected activeMachine: Machine | null = null;

  protected readonly menuItems = computed<MenuItem[]>(() => [
    ...(this.canReadMetrics()
      ? [
          {
            label: this._labels()[MENU_METRICS_KEY] ?? MENU_METRICS_KEY,
            icon: 'pi pi-chart-line',
            command: (): void => this._emitMetrics(),
          },
        ]
      : []),
    ...(this.canReadStatuses()
      ? [
          {
            label: this._labels()[MENU_STATUSES_KEY] ?? MENU_STATUSES_KEY,
            icon: 'pi pi-list',
            command: (): void => this._emitStatuses(),
          },
        ]
      : []),
    ...(this.canReadFiles()
      ? [
          {
            label: this._labels()[MENU_FILES_KEY] ?? MENU_FILES_KEY,
            icon: 'pi pi-folder',
            command: (): void => this._emitFiles(),
          },
        ]
      : []),
    ...(this.canWrite()
      ? [
          {
            label: this._labels()[MENU_UPDATE_KEY] ?? MENU_UPDATE_KEY,
            icon: 'pi pi-pencil',
            command: (): void => this._emitUpdate(),
          },
        ]
      : []),
    ...(this.canDelete()
      ? [
          {
            label: this._labels()[MENU_DELETE_KEY] ?? MENU_DELETE_KEY,
            icon: 'pi pi-trash',
            command: (): void => this._emitDelete(),
          },
        ]
      : []),
  ]);

  protected columnLabelKey(column: MachineColumn): string {
    return COLUMN_LABEL_KEYS[column];
  }

  protected isSortable(column: MachineColumn): boolean {
    return SORTABLE_COLUMNS.includes(column);
  }

  protected stateLabel(state: MachineState): string {
    return this._labels()[STATE_LABEL_KEYS[state]] ?? STATE_LABEL_KEYS[state];
  }

  protected isColumnVisible(column: MachineColumn): boolean {
    return this.columns().includes(column);
  }

  protected onColumnToggle(column: MachineColumn, isVisible: boolean): void {
    this.columnsChange.emit(toggleColumn(this.columns(), column, isVisible));
  }

  protected onFilterApplied(event: TableFilterEvent): void {
    this.filterChange.emit(readTableFilters(this.allColumns, FILTER_READERS, event.filters));
  }

  protected onFilterHidden(column: MachineColumn): void {
    const table = this.table();
    table.filters = { ...table.filters, [column]: [toConstraint(FILTER_WRITERS[column](this.filter()))] };
  }

  protected onPage(event: TablePageEvent): void {
    this.pageChange.emit({ offset: event.first, limit: event.rows });
  }

  protected onSort(event: { field?: string | null; order?: number | null }): void {
    const column = SORTABLE_COLUMNS.find((candidate) => candidate === event.field) as MachineSortColumn | undefined;
    this.sortChange.emit(
      column === undefined
        ? null
        : {
            column,
            direction: toSortDirection(event.order),
          },
    );
  }

  protected onColumnReorder(event: TableColumnReorderEvent): void {
    this.columnsReorder.emit((event.columns ?? []) as MachineColumn[]);
  }

  protected openMenu(event: Event, machine: Machine, menu: Menu): void {
    this.activeMachine = machine;
    menu.toggle(event);
  }

  private _emitMetrics(): void {
    if (this.activeMachine !== null) {
      this.metricsRequested.emit(this.activeMachine.id);
    }
  }

  private _emitStatuses(): void {
    if (this.activeMachine !== null) {
      this.statusesRequested.emit(this.activeMachine.id);
    }
  }

  private _emitFiles(): void {
    if (this.activeMachine !== null) {
      this.filesRequested.emit(this.activeMachine.id);
    }
  }

  private _emitUpdate(): void {
    if (this.activeMachine !== null) {
      this.updateRequested.emit(this.activeMachine.id);
    }
  }

  private _emitDelete(): void {
    if (this.activeMachine !== null) {
      this.deleteRequested.emit(this.activeMachine);
    }
  }
}
