import { Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { Badge } from 'primeng/badge';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Menu } from 'primeng/menu';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { ColumnFilter, SortIcon, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';

import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import type { GridSort } from '@shared/grid/requests/grid-request';
import { ColumnChooser } from '@shared/grid/table/column-chooser/column-chooser';
import { ChipSummary } from '@shared/chips/chip-summary/chip-summary';
import { labelChipsOf } from '@shared/chips/chip-summary/chip-summary.utils';
import { createGridTable } from '@shared/grid/table/grid-table';
import { createRowMenu } from '@shared/grid/table/row-menu';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';
import { PlainDateRangePicker } from '@shared/time/pickers/plain-date-range-picker/plain-date-range-picker';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import {
  type Machine,
  MachineColumn,
  type MachineFilter,
  type MachineSort,
  type MachineSortColumn,
  MachineState,
} from '@machines/machine';
import {
  COLUMN_LABEL_KEYS,
  DRAFTS_NO_KEY,
  DRAFTS_YES_KEY,
  FILTER_READERS,
  FILTER_WRITERS,
  MENU_DELETE_KEY,
  MENU_DELETE_TOOLTIP_KEY,
  MENU_DRAFTS_KEY,
  MENU_DRAFTS_TOOLTIP_KEY,
  MENU_FILES_KEY,
  MENU_FILES_TOOLTIP_KEY,
  MENU_METRICS_KEY,
  MENU_METRICS_TOOLTIP_KEY,
  MENU_OPEN_KEY,
  MENU_OPEN_TOOLTIP_KEY,
  MENU_STATUSES_KEY,
  MENU_STATUSES_TOOLTIP_KEY,
  SORTABLE_COLUMNS,
  STATE_LABEL_KEYS,
} from './machine-grid.utils';

@Component({
  imports: [
    ChipSummary,
    Badge,
    ButtonDirective,
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
    TableModule,
    Tooltip,
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
  readonly hasLoaded = input(false);
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
  readonly canReadDrafts = input(false);

  readonly filterChange = output<Partial<MachineFilter>>();
  readonly sortChange = output<GridSort<MachineSortColumn> | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly MachineColumn[]>();
  readonly columnsReorder = output<readonly MachineColumn[]>();
  readonly updateRequested = output<string>();
  readonly deleteRequested = output<Machine>();
  readonly statusesRequested = output<string>();
  readonly filesRequested = output<string>();
  readonly metricsRequested = output<string>();
  readonly draftsRequested = output<string>();

  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  protected readonly MachineColumn = MachineColumn;
  protected readonly allColumns = Object.values(MachineColumn);
  protected readonly columnLabelKeys = COLUMN_LABEL_KEYS;
  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly emptyRange = EMPTY_RANGE;

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      ...Object.values(STATE_LABEL_KEYS),
      MENU_OPEN_KEY,
      MENU_METRICS_KEY,
      MENU_DRAFTS_KEY,
      DRAFTS_YES_KEY,
      DRAFTS_NO_KEY,
      MENU_STATUSES_KEY,
      MENU_FILES_KEY,
      MENU_DELETE_KEY,
      MENU_OPEN_TOOLTIP_KEY,
      MENU_METRICS_TOOLTIP_KEY,
      MENU_STATUSES_TOOLTIP_KEY,
      MENU_DRAFTS_TOOLTIP_KEY,
      MENU_FILES_TOOLTIP_KEY,
      MENU_DELETE_TOOLTIP_KEY,
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly grid = createGridTable<Machine, MachineColumn, MachineFilter, MachineSortColumn>({
    allColumns: this.allColumns,
    columns: this.columns,
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

  protected readonly rowMenu = createRowMenu<Machine>();
  protected readonly stateOptions = computed(() =>
    Object.values(MachineState).map((state) => ({
      value: state,
      label: this._labels()[STATE_LABEL_KEYS[state]] ?? STATE_LABEL_KEYS[state],
    })),
  );
  protected readonly draftsOptions = computed(() => [
    { value: true, label: this._labels()[DRAFTS_YES_KEY] ?? DRAFTS_YES_KEY },
    { value: false, label: this._labels()[DRAFTS_NO_KEY] ?? DRAFTS_NO_KEY },
  ]);
  protected readonly labelChips = labelChipsOf;
  protected readonly tableStyle = computed(() => ({
    'min-width': '64rem',
    ...(this.grid.tableRows().length > 0 ? {} : { height: '100%' }),
  }));
  protected readonly hasRowActions = computed(
    () =>
      this.canWrite() ||
      this.canDelete() ||
      this.canReadStatuses() ||
      this.canReadFiles() ||
      this.canReadMetrics() ||
      this.canReadDrafts(),
  );

  protected readonly menuItems = computed<MenuItem[]>(() => {
    const labels = this._labels();
    return [
      ...(this.canWrite()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_OPEN_KEY,
                tooltipKey: MENU_OPEN_TOOLTIP_KEY,
                icon: 'pi pi-external-link',
                onSelect: (machine) => this.updateRequested.emit(machine.id),
              },
              labels,
            ),
          ]
        : []),
      ...(this.canReadMetrics()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_METRICS_KEY,
                tooltipKey: MENU_METRICS_TOOLTIP_KEY,
                icon: 'pi pi-chart-line',
                onSelect: (machine) => this.metricsRequested.emit(machine.id),
              },
              labels,
            ),
          ]
        : []),
      ...(this.canReadStatuses()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_STATUSES_KEY,
                tooltipKey: MENU_STATUSES_TOOLTIP_KEY,
                icon: 'pi pi-list',
                onSelect: (machine) => this.statusesRequested.emit(machine.id),
              },
              labels,
            ),
          ]
        : []),
      ...(this.canReadDrafts()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_DRAFTS_KEY,
                tooltipKey: MENU_DRAFTS_TOOLTIP_KEY,
                icon: 'pi pi-inbox',
                onSelect: (machine) => this.draftsRequested.emit(machine.id),
              },
              labels,
            ),
          ]
        : []),
      ...(this.canReadFiles()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_FILES_KEY,
                tooltipKey: MENU_FILES_TOOLTIP_KEY,
                icon: 'pi pi-folder',
                onSelect: (machine) => this.filesRequested.emit(machine.id),
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
                onSelect: (machine) => this.deleteRequested.emit(machine),
              },
              labels,
            ),
          ]
        : []),
    ];
  });

  protected stateLabel(state: MachineState): string {
    return this._labels()[STATE_LABEL_KEYS[state]] ?? STATE_LABEL_KEYS[state];
  }
}
