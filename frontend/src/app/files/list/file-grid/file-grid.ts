import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';
import { Menu } from 'primeng/menu';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { ColumnFilter, SortIcon, TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { FileTypeTag } from '@shared/files/file-type-tag/file-type-tag';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import type { GridSort } from '@shared/grid/requests/grid-request';
import { ColumnChooser } from '@shared/grid/table/column-chooser/column-chooser';
import { ChipSummary } from '@shared/chips/chip-summary/chip-summary';
import { type Chip, labelChipsOf, toChips } from '@shared/chips/chip-summary/chip-summary.utils';
import { createGridTable } from '@shared/grid/table/grid-table';
import { createRowMenu } from '@shared/grid/table/row-menu';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { PlainDateRangePicker } from '@shared/time/pickers/plain-date-range-picker/plain-date-range-picker';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import { FileColumn, type FileFilter, type FileGridItem, type FileSort, type FileSortColumn } from '@files/file';
import { formatFileSize } from '@files/file.utils';
import { IngestState } from '@files/ingest/ingest-attempt';
import type { MachineOption } from '@files/file';
import {
  COLUMN_LABEL_KEYS,
  EXTENSION_OPTIONS,
  type FileRowAccess,
  type FileRowAction,
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
  MENU_DELETE_TOOLTIP_KEY,
  MENU_DOWNLOAD_KEY,
  MENU_DOWNLOAD_TOOLTIP_KEY,
  MENU_EDIT_KEY,
  MENU_EDIT_TOOLTIP_KEY,
  NEVER_OPTION,
  NO_ROW_ACCESS,
  NOT_TIED_OPTION,
  ROW_ACTION_ICONS,
  ROW_ACTION_KEYS,
  ROW_ACTION_TOOLTIP_KEYS,
  SORTABLE_COLUMNS,
  visibleRowActions,
} from './file-grid.utils';

const ALL_COLUMNS: readonly FileColumn[] = Object.values(FileColumn);

@Component({
  imports: [
    ChipSummary,
    ButtonDirective,
    Checkbox,
    ColumnChooser,
    ColumnFilter,
    EmptyState,
    FileTypeTag,
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
  selector: 'hf-file-grid',
  templateUrl: './file-grid.html',
})
export class FileGrid {
  readonly rows = input.required<readonly FileGridItem[]>();
  readonly isSelectable = input(false);
  readonly selectedIds = input<readonly string[]>([]);
  readonly lockedIds = input<readonly string[]>([]);
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
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
  readonly sortChange = output<GridSort<FileSortColumn> | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly FileColumn[]>();
  readonly columnsReorder = output<readonly FileColumn[]>();
  readonly selectionToggled = output<{ id: string; isSelected: boolean }>();
  readonly downloadRequested = output<FileGridItem>();
  readonly editRequested = output<FileGridItem>();
  readonly deleteRequested = output<FileGridItem>();
  readonly rowActionRequested = output<{ action: FileRowAction; file: FileGridItem }>();
  readonly machineSearch = output<string>();

  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  protected readonly FileColumn = FileColumn;
  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly extensionOptions = EXTENSION_OPTIONS;
  protected readonly columnLabelKeys = COLUMN_LABEL_KEYS;
  protected readonly formatFileSize = formatFileSize;
  protected readonly ingestLabelKey = ingestLabelKey;
  protected readonly ingestSeverity = ingestSeverity;
  protected readonly emptyRange = EMPTY_RANGE;

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      MENU_DOWNLOAD_KEY,
      MENU_EDIT_KEY,
      MENU_DELETE_KEY,
      MENU_DOWNLOAD_TOOLTIP_KEY,
      MENU_EDIT_TOOLTIP_KEY,
      MENU_DELETE_TOOLTIP_KEY,
      FILTER_NOT_TIED_KEY,
      INGEST_NEVER_LABEL_KEY,
      ...Object.values(ROW_ACTION_KEYS),
      ...Object.values(ROW_ACTION_TOOLTIP_KEYS),
      ...Object.values(INGEST_STATE_LABEL_KEYS),
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly allColumns = computed(() =>
    ALL_COLUMNS.filter((column) => this.showIngestColumns() || !INGEST_COLUMNS.includes(column)),
  );

  protected readonly grid = createGridTable<FileGridItem, FileColumn, FileFilter, FileSortColumn>({
    allColumns: ALL_COLUMNS,
    columns: computed(() => this.columns().filter((column) => this.allColumns().includes(column))),
    filter: this.filter,
    sort: this.sort,
    limit: this.limit,
    isLoading: this.isLoading,
    hasLoaded: this.hasLoaded,
    rows: this.rows,
    filterReaders: FILTER_READERS,
    filterWriters: FILTER_WRITERS,
    sortableColumns: SORTABLE_COLUMNS,
    labelKeys: COLUMN_LABEL_KEYS,
    filterChange: (patch) => this.filterChange.emit(patch),
    sortChange: (sort) => this.sortChange.emit(sort),
    pageChange: (page) => this.pageChange.emit(page),
    columnsChange: (columns) => this.columnsChange.emit(columns),
    columnsReorder: (columns) => this.columnsReorder.emit(columns),
  });

  protected readonly rowMenu = createRowMenu<FileGridItem>();
  protected readonly menuItems = signal<MenuItem[]>([]);
  protected readonly machineFilterOptions = computed(() => [
    { value: NOT_TIED_OPTION, label: this._labels()[FILTER_NOT_TIED_KEY] ?? FILTER_NOT_TIED_KEY },
    ...this.machineOptions().map((machine) => ({ value: machine.id, label: machine.name })),
  ]);
  protected readonly ingestFilterOptions = computed(() => [
    {
      value: NEVER_OPTION,
      label: this._labels()[INGEST_NEVER_LABEL_KEY] ?? INGEST_NEVER_LABEL_KEY,
      severity: ingestSeverity(null),
    },
    ...Object.values(IngestState).map((state) => ({
      value: state,
      label: this._labels()[INGEST_STATE_LABEL_KEYS[state]] ?? INGEST_STATE_LABEL_KEYS[state],
      severity: ingestSeverity(state),
    })),
  ]);
  protected readonly labelChips = labelChipsOf;
  protected readonly tableStyle = computed(() => ({
    'min-width': '48rem',
    ...(this.grid.tableRows().length > 0 ? {} : { height: '100%' }),
  }));

  protected isFilterable(column: FileColumn): boolean {
    return FILTERABLE_COLUMNS.includes(column);
  }

  protected isChecked(id: string): boolean {
    return this.selectedIds().includes(id) || this.lockedIds().includes(id);
  }

  protected asExtensions(value: unknown): string[] {
    return Array.isArray(value) ? (value as string[]) : [];
  }

  protected typeChips(extensions: readonly string[]): Chip[] {
    return toChips(
      extensions,
      'hf-file-type-',
      (extension) => extension.toUpperCase(),
      EXTENSION_OPTIONS.map((option) => option.value),
    );
  }

  /** Builds the menu of the row at the click, so the texts and the row actions follow the row and its permissions. */
  protected openMenu(event: Event, file: FileGridItem, menu: Menu): void {
    this.menuItems.set(this._menuItemsFor(file));
    this.rowMenu.open(event, file, menu);
  }

  private _menuItemsFor(file: FileGridItem): MenuItem[] {
    const labels = this._labels();
    const baseItems: MenuItem[] = [
      this.rowMenu.menuItem(
        {
          labelKey: MENU_DOWNLOAD_KEY,
          tooltipKey: MENU_DOWNLOAD_TOOLTIP_KEY,
          icon: 'pi pi-download',
          onSelect: (row) => this.downloadRequested.emit(row),
        },
        labels,
      ),
      ...(this.canEdit()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_EDIT_KEY,
                tooltipKey: MENU_EDIT_TOOLTIP_KEY,
                icon: 'pi pi-pencil',
                onSelect: (row) => this.editRequested.emit(row),
              },
              labels,
            ),
          ]
        : []),
      ...(this.canDelete()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_DELETE_KEY,
                tooltipKey: MENU_DELETE_TOOLTIP_KEY,
                icon: 'pi pi-trash',
                onSelect: (row) => this.deleteRequested.emit(row),
              },
              labels,
            ),
          ]
        : []),
    ];
    const rowActionItems = visibleRowActions(file, this.access()).map((action) =>
      this.rowMenu.menuItem(
        {
          labelKey: ROW_ACTION_KEYS[action],
          tooltipKey: ROW_ACTION_TOOLTIP_KEYS[action],
          icon: ROW_ACTION_ICONS[action],
          onSelect: (row) => this.rowActionRequested.emit({ action, file: row }),
        },
        labels,
      ),
    );
    return [...baseItems, ...rowActionItems];
  }
}
