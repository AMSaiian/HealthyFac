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
import {
  ColumnFilter,
  SortIcon,
  Table,
  type TableColumnReorderEvent,
  type TableFilterEvent,
  TableModule,
  type TablePageEvent,
} from 'primeng/table';
import { Tag } from 'primeng/tag';

import { toggleColumn } from '@shared/grid/table/grid-columns';
import { keepWhileLoading, skeletonRows } from '@shared/grid/table/grid-display';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { toSortDirection, toSortOrder } from '@shared/grid/table/grid-sort-order';
import { readTableFilters, toConstraint, toTableFilters } from '@shared/grid/table/table-filters';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { PlainDateRangePicker } from '@shared/time/pickers/plain-date-range-picker/plain-date-range-picker';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import { FileColumn, type FileFilter, type FileGridItem, type FileSort, type FileSortColumn } from '@files/file';
import { formatFileSize } from '@files/file.utils';
import { IngestState } from '@files/ingest/ingest-attempt';
import type { MachineOption } from '@files/file';
import type { FileRowAction } from './file-grid.utils';
import {
  COLUMN_LABEL_KEYS,
  EXTENSION_OPTIONS,
  type FileRowAccess,
  FILTER_NOT_TIED_KEY,
  FILTER_READERS,
  FILTER_WRITERS,
  FILTERABLE_COLUMNS,
  INGEST_COLUMNS,
  INGEST_NEVER_LABEL_KEY,
  INGEST_STATE_LABEL_KEYS,
  ingestLabelKey,
  ingestSeverity,
  MENU_DELETE_KEY,
  MENU_DOWNLOAD_KEY,
  MENU_EDIT_KEY,
  NEVER_OPTION,
  NO_ROW_ACCESS,
  NOT_TIED_OPTION,
  ROW_ACTION_ICONS,
  ROW_ACTION_KEYS,
  SORTABLE_COLUMNS,
  visibleRowActions,
} from './file-grid.utils';

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
    Tag,
    TableModule,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-file-grid',
  templateUrl: './file-grid.html',
})
export class FileGrid {
  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  readonly rows = input.required<readonly FileGridItem[]>();
  readonly isSelectable = input(false);
  readonly selectedIds = input<readonly string[]>([]);
  readonly lockedIds = input<readonly string[]>([]);
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly columns = input.required<readonly FileColumn[]>();
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<FileSort | null>();
  readonly filter = input.required<FileFilter>();
  readonly canEdit = input(false);
  readonly canDelete = input(false);
  /** The company page shows the machine and ingest columns and their row actions; the machine page hides them. */
  readonly showIngestColumns = input(true);
  readonly machineOptions = input<readonly MachineOption[]>([]);
  readonly access = input<FileRowAccess>(NO_ROW_ACCESS);

  readonly filterChange = output<Partial<FileFilter>>();
  readonly sortChange = output<FileSort | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly FileColumn[]>();
  readonly columnsReorder = output<readonly FileColumn[]>();
  readonly selectionToggled = output<{ id: string; isSelected: boolean }>();
  readonly downloadRequested = output<FileGridItem>();
  readonly editRequested = output<FileGridItem>();
  readonly deleteRequested = output<FileGridItem>();
  readonly rowActionRequested = output<{ action: FileRowAction; file: FileGridItem }>();
  readonly machineSearch = output<string>();

  protected readonly table = viewChild.required(Table);
  protected readonly FileColumn = FileColumn;
  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly extensionOptions = EXTENSION_OPTIONS;
  protected readonly formatFileSize = formatFileSize;
  protected readonly ingestLabelKey = ingestLabelKey;
  protected readonly ingestSeverity = ingestSeverity;

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      MENU_DOWNLOAD_KEY,
      MENU_EDIT_KEY,
      MENU_DELETE_KEY,
      FILTER_NOT_TIED_KEY,
      INGEST_NEVER_LABEL_KEY,
      ...Object.values(ROW_ACTION_KEYS),
      ...Object.values(INGEST_STATE_LABEL_KEYS),
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly allColumns = computed(() =>
    Object.values(FileColumn).filter((column) => this.showIngestColumns() || !INGEST_COLUMNS.includes(column)),
  );
  protected readonly machineFilterOptions = computed(() => [
    { value: NOT_TIED_OPTION, label: this._labels()[FILTER_NOT_TIED_KEY] ?? FILTER_NOT_TIED_KEY },
    ...this.machineOptions().map((machine) => ({ value: machine.id, label: machine.name })),
  ]);
  protected readonly ingestFilterOptions = computed(() => [
    { value: NEVER_OPTION, label: this._labels()[INGEST_NEVER_LABEL_KEY] ?? INGEST_NEVER_LABEL_KEY },
    ...Object.values(IngestState).map((state) => ({
      value: state,
      label: this._labels()[INGEST_STATE_LABEL_KEYS[state]] ?? INGEST_STATE_LABEL_KEYS[state],
    })),
  ]);

  protected readonly sortOrder = computed(() => toSortOrder(this.sort()?.direction));
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly keptRows = keepWhileLoading(this.rows, this.isLoading);
  protected readonly skeletonRows = computed(() => skeletonRows(this.limit()));
  protected readonly emptyRange = EMPTY_RANGE;
  protected readonly tableFilters = computed(() => toTableFilters(this.allColumns(), FILTER_WRITERS, this.filter()));
  protected readonly tableRows = computed((): unknown[] =>
    this.showSkeleton() ? this.skeletonRows() : [...this.keptRows()],
  );
  protected readonly tableStyle = computed(() => ({
    'min-width': '48rem',
    ...(this.showSkeleton() || this.keptRows().length > 0 ? {} : { height: '100%' }),
  }));
  protected readonly tableColumns = computed(() =>
    this.columns().filter((column) => this.allColumns().includes(column)),
  );

  protected readonly activeFile = signal<FileGridItem | null>(null);

  protected readonly menuItems = computed<MenuItem[]>(() => {
    const file = this.activeFile();
    const rowActions = file === null ? [] : visibleRowActions(file, this.access());
    return [...this._baseItems(), ...this._rowActionItems(rowActions)];
  });

  private readonly _baseItems = computed<MenuItem[]>(() => [
    {
      label: this._labels()[MENU_DOWNLOAD_KEY] ?? MENU_DOWNLOAD_KEY,
      icon: 'pi pi-download',
      command: (): void => this._emit(this.downloadRequested),
    },
    ...(this.canEdit()
      ? [
          {
            label: this._labels()[MENU_EDIT_KEY] ?? MENU_EDIT_KEY,
            icon: 'pi pi-pencil',
            command: (): void => this._emit(this.editRequested),
          },
        ]
      : []),
    ...(this.canDelete()
      ? [
          {
            label: this._labels()[MENU_DELETE_KEY] ?? MENU_DELETE_KEY,
            icon: 'pi pi-trash',
            command: (): void => this._emit(this.deleteRequested),
          },
        ]
      : []),
  ]);

  protected columnLabelKey(column: FileColumn): string {
    return COLUMN_LABEL_KEYS[column];
  }

  protected isSortable(column: FileColumn): boolean {
    return SORTABLE_COLUMNS.includes(column);
  }

  protected isFilterable(column: FileColumn): boolean {
    return FILTERABLE_COLUMNS.includes(column);
  }

  protected isChecked(id: string): boolean {
    return this.selectedIds().includes(id) || this.lockedIds().includes(id);
  }

  protected isColumnVisible(column: FileColumn): boolean {
    return this.tableColumns().includes(column);
  }

  protected onColumnToggle(column: FileColumn, isVisible: boolean): void {
    this.columnsChange.emit(toggleColumn(this.columns(), column, isVisible));
  }

  protected onFilterApplied(event: TableFilterEvent): void {
    this.filterChange.emit(readTableFilters(this.allColumns(), FILTER_READERS, event.filters));
  }

  protected onFilterHidden(column: FileColumn): void {
    const table = this.table();
    table.filters = { ...table.filters, [column]: [toConstraint(FILTER_WRITERS[column](this.filter()))] };
  }

  protected onPage(event: TablePageEvent): void {
    this.pageChange.emit({ offset: event.first, limit: event.rows });
  }

  protected onSort(event: { field?: string | null; order?: number | null }): void {
    const column = SORTABLE_COLUMNS.find((candidate) => candidate === event.field) as FileSortColumn | undefined;
    this.sortChange.emit(column === undefined ? null : { column, direction: toSortDirection(event.order) });
  }

  protected onColumnReorder(event: TableColumnReorderEvent): void {
    this.columnsReorder.emit((event.columns ?? []) as FileColumn[]);
  }

  protected openMenu(event: Event, file: FileGridItem, menu: Menu): void {
    this.activeFile.set(file);
    menu.toggle(event);
  }

  private _rowActionItems(actions: readonly FileRowAction[]): MenuItem[] {
    return actions.map((action) => ({
      label: this._labels()[ROW_ACTION_KEYS[action]] ?? ROW_ACTION_KEYS[action],
      icon: ROW_ACTION_ICONS[action],
      command: (): void => {
        const file = this.activeFile();
        if (file !== null) {
          this.rowActionRequested.emit({ action, file });
        }
      },
    }));
  }

  private _emit(target: { emit(value: FileGridItem): void }): void {
    const file = this.activeFile();
    if (file !== null) {
      target.emit(file);
    }
  }
}
