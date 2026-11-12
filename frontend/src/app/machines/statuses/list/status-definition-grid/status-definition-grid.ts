import { Component, computed, inject, input, output } from '@angular/core';
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

import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import type { GridSort } from '@shared/grid/requests/grid-request';
import { ColumnChooser } from '@shared/grid/table/column-chooser/column-chooser';
import { ChipSummary } from '@shared/chips/chip-summary/chip-summary';
import { labelChipsOf } from '@shared/chips/chip-summary/chip-summary.utils';
import { createGridTable } from '@shared/grid/table/grid-table';
import { createRowMenu } from '@shared/grid/table/row-menu';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { PlainDateRangePicker } from '@shared/time/pickers/plain-date-range-picker/plain-date-range-picker';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

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
  MENU_DELETE_TOOLTIP_KEY,
  MENU_UPDATE_KEY,
  MENU_UPDATE_TOOLTIP_KEY,
  SEVERITY_LABEL_KEYS,
  SORTABLE_COLUMNS,
} from './status-definition-grid.utils';

@Component({
  imports: [
    ChipSummary,
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
  selector: 'hf-status-definition-grid',
  templateUrl: './status-definition-grid.html',
})
export class StatusDefinitionGrid {
  readonly rows = input.required<readonly StatusDefinition[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
  readonly columns = input.required<readonly StatusDefinitionColumn[]>();
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<StatusDefinitionSort | null>();
  readonly filter = input.required<StatusDefinitionFilter>();
  readonly canWrite = input(false);
  readonly canDelete = input(false);

  readonly filterChange = output<Partial<StatusDefinitionFilter>>();
  readonly sortChange = output<GridSort<StatusDefinitionSortColumn> | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly StatusDefinitionColumn[]>();
  readonly columnsReorder = output<readonly StatusDefinitionColumn[]>();
  readonly updateRequested = output<string>();
  readonly deleteRequested = output<StatusDefinition>();

  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  protected readonly StatusDefinitionColumn = StatusDefinitionColumn;
  protected readonly allColumns = Object.values(StatusDefinitionColumn);
  protected readonly columnLabelKeys = COLUMN_LABEL_KEYS;
  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly emptyRange = EMPTY_RANGE;

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      MENU_UPDATE_KEY,
      MENU_DELETE_KEY,
      MENU_UPDATE_TOOLTIP_KEY,
      MENU_DELETE_TOOLTIP_KEY,
      ...Object.values(SEVERITY_LABEL_KEYS),
    ]),
    {
      initialValue: {} as Record<string, string>,
    },
  );

  protected readonly grid = createGridTable<
    StatusDefinition,
    StatusDefinitionColumn,
    StatusDefinitionFilter,
    StatusDefinitionSortColumn
  >({
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

  protected readonly rowMenu = createRowMenu<StatusDefinition>();
  protected readonly severityOptions = computed(() =>
    Object.values(StatusSeverity).map((severity) => ({
      value: severity,
      label: this._labels()[SEVERITY_LABEL_KEYS[severity]] ?? SEVERITY_LABEL_KEYS[severity],
    })),
  );
  protected readonly labelChips = labelChipsOf;
  protected readonly tableStyle = computed(() => ({
    'min-width': '40rem',
    ...(this.grid.tableRows().length > 0 ? {} : { height: '100%' }),
  }));
  protected readonly hasRowActions = computed(() => this.canWrite() || this.canDelete());

  protected readonly menuItems = computed<MenuItem[]>(() => {
    const labels = this._labels();
    return [
      ...(this.canWrite()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: MENU_UPDATE_KEY,
                tooltipKey: MENU_UPDATE_TOOLTIP_KEY,
                icon: 'pi pi-pencil',
                onSelect: (statusDefinition) => this.updateRequested.emit(statusDefinition.id),
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
                onSelect: (statusDefinition) => this.deleteRequested.emit(statusDefinition),
              },
              labels,
            ),
          ]
        : []),
    ];
  });

  protected severityLabel(severity: StatusSeverity): string {
    return this._labels()[SEVERITY_LABEL_KEYS[severity]] ?? SEVERITY_LABEL_KEYS[severity];
  }
}
