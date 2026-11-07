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

import { toggleColumn } from '@shared/grid/table/grid-columns';
import { skeletonRows } from '@shared/grid/table/grid-display';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { toSortDirection, toSortOrder } from '@shared/grid/table/grid-sort-order';
import { readTableFilters, toConstraint, toTableFilters } from '@shared/grid/table/table-filters';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { PlainDateRangePicker } from '@shared/time/pickers/plain-date-range-picker/plain-date-range-picker';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';

import {
  type StatusDefinition,
  StatusDefinitionColumn,
  type StatusDefinitionFilter,
  type StatusDefinitionSort,
  type StatusDefinitionSortColumn,
  StatusSeverity,
} from '@machines/statuses/status-definition';
import {
  COLUMN_LABEL_KEYS,
  FILTER_READERS,
  FILTER_WRITERS,
  MENU_DELETE_KEY,
  MENU_UPDATE_KEY,
  SEVERITY_LABEL_KEYS,
  SORTABLE_COLUMNS,
} from './status-definition-grid.utils';

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
  selector: 'hf-status-definition-grid',
  templateUrl: './status-definition-grid.html',
})
export class StatusDefinitionGrid {
  readonly rows = input.required<readonly StatusDefinition[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly columns = input.required<readonly StatusDefinitionColumn[]>();
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<StatusDefinitionSort | null>();
  readonly filter = input.required<StatusDefinitionFilter>();
  readonly canWrite = input(false);
  readonly canDelete = input(false);

  readonly filterChange = output<Partial<StatusDefinitionFilter>>();
  readonly sortChange = output<StatusDefinitionSort | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly StatusDefinitionColumn[]>();
  readonly columnsReorder = output<readonly StatusDefinitionColumn[]>();
  readonly updateRequested = output<string>();
  readonly deleteRequested = output<StatusDefinition>();

  protected readonly table = viewChild.required(Table);
  protected readonly StatusDefinitionColumn = StatusDefinitionColumn;
  protected readonly allColumns = Object.values(StatusDefinitionColumn);
  protected readonly pageSizes = [...PAGE_SIZES];

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([MENU_UPDATE_KEY, MENU_DELETE_KEY, ...Object.values(SEVERITY_LABEL_KEYS)]),
    {
      initialValue: {} as Record<string, string>,
    },
  );

  protected readonly severityOptions = computed(() =>
    Object.values(StatusSeverity).map((severity) => ({
      value: severity,
      label: this._labels()[SEVERITY_LABEL_KEYS[severity]] ?? SEVERITY_LABEL_KEYS[severity],
    })),
  );
  protected readonly sortOrder = computed(() => toSortOrder(this.sort()?.direction));
  protected readonly skeletonRows = computed(() => skeletonRows(this.limit()));
  protected readonly emptyRange = EMPTY_RANGE;
  protected readonly tableFilters = computed(() => toTableFilters(this.allColumns, FILTER_WRITERS, this.filter()));
  protected readonly tableRows = computed((): unknown[] => (this.isLoading() ? this.skeletonRows() : [...this.rows()]));
  protected readonly tableStyle = computed(() => ({
    'min-width': '40rem',
    ...(this.isLoading() || this.rows().length > 0 ? {} : { height: '100%' }),
  }));
  protected readonly tableColumns = computed(() => [...this.columns()]);
  protected readonly hasRowActions = computed(() => this.canWrite() || this.canDelete());

  protected activeStatusDefinition: StatusDefinition | null = null;

  protected readonly menuItems = computed<MenuItem[]>(() => [
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

  protected severityLabel(severity: StatusSeverity): string {
    return this._labels()[SEVERITY_LABEL_KEYS[severity]] ?? SEVERITY_LABEL_KEYS[severity];
  }

  protected columnLabelKey(column: StatusDefinitionColumn): string {
    return COLUMN_LABEL_KEYS[column];
  }

  protected isSortable(column: StatusDefinitionColumn): boolean {
    return SORTABLE_COLUMNS.includes(column);
  }

  protected isColumnVisible(column: StatusDefinitionColumn): boolean {
    return this.columns().includes(column);
  }

  protected onColumnToggle(column: StatusDefinitionColumn, isVisible: boolean): void {
    this.columnsChange.emit(toggleColumn(this.columns(), column, isVisible));
  }

  protected onFilterApplied(event: TableFilterEvent): void {
    this.filterChange.emit(readTableFilters(this.allColumns, FILTER_READERS, event.filters));
  }

  protected onFilterHidden(column: StatusDefinitionColumn): void {
    const table = this.table();
    table.filters = { ...table.filters, [column]: [toConstraint(FILTER_WRITERS[column](this.filter()))] };
  }

  protected onPage(event: TablePageEvent): void {
    this.pageChange.emit({ offset: event.first, limit: event.rows });
  }

  protected onSort(event: { field?: string | null; order?: number | null }): void {
    const column = SORTABLE_COLUMNS.find((candidate) => candidate === event.field) as
      StatusDefinitionSortColumn | undefined;
    this.sortChange.emit(column === undefined ? null : { column, direction: toSortDirection(event.order) });
  }

  protected onColumnReorder(event: TableColumnReorderEvent): void {
    this.columnsReorder.emit((event.columns ?? []) as StatusDefinitionColumn[]);
  }

  protected openMenu(event: Event, statusDefinition: StatusDefinition, menu: Menu): void {
    this.activeStatusDefinition = statusDefinition;
    menu.toggle(event);
  }

  private _emitUpdate(): void {
    if (this.activeStatusDefinition !== null) {
      this.updateRequested.emit(this.activeStatusDefinition.id);
    }
  }

  private _emitDelete(): void {
    if (this.activeStatusDefinition !== null) {
      this.deleteRequested.emit(this.activeStatusDefinition);
    }
  }
}
