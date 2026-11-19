import { Component, computed, inject, input, output, viewChild } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';

import { ColumnFilter, SortIcon, Table, TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { ChipSummary } from '@shared/chips/chip-summary/chip-summary';
import { type Chip, labelChipsOf, lookupChips } from '@shared/chips/chip-summary/chip-summary.utils';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import type { GridSort } from '@shared/grid/requests/grid-request';
import { ColumnChooser } from '@shared/grid/table/column-chooser/column-chooser';
import { CellText } from '@shared/grid/table/cell-text/cell-text';
import { createGridTable } from '@shared/grid/table/grid-table';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import {
  type LookupOption,
  type LookupRequest,
  type MachineLabel,
  machineLabelOf,
} from '@shared/lookups/lookup-options';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import {
  AgentRunState,
  CaseColumn,
  type CaseFilter,
  type CaseListItem,
  type CaseSortColumn,
  CaseState,
  SignalSeverity,
} from '@maintenance/case';
import {
  AI_UNAVAILABLE_KEY,
  AI_UNAVAILABLE_OPTION,
  COLUMN_LABEL_KEYS,
  FILTER_READERS,
  FILTER_WRITERS,
  FILTERABLE_COLUMNS,
  OPEN_TOOLTIP_KEY,
  RUN_STATE_LABEL_KEYS,
  SEVERITY_LABEL_KEYS,
  SORTABLE_COLUMNS,
  STATE_LABEL_KEYS,
  TEXT_COLUMNS,
  UNKNOWN_MACHINE_KEY,
} from '@maintenance/list/cases-grid/cases-grid.utils';

@Component({
  imports: [
    CellText,
    ButtonDirective,
    ChipSummary,
    ColumnChooser,
    ColumnFilter,
    EmptyState,
    FormsModule,
    LocalDateTimePipe,
    Select,
    Skeleton,
    SortIcon,
    TableModule,
    Tag,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-cases-grid',
  templateUrl: './cases-grid.html',
})
export class CasesGrid {
  readonly rows = input.required<readonly CaseListItem[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
  readonly columns = input.required<readonly CaseColumn[]>();
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<GridSort<CaseSortColumn> | null>();
  readonly filter = input.required<CaseFilter>();
  readonly machineLabels = input.required<ReadonlyMap<string, MachineLabel>>();
  readonly machines = input<LookupOption[]>([]);
  readonly isMachinesLoading = input(false);

  readonly filterChange = output<Partial<CaseFilter>>();
  readonly sortChange = output<GridSort<CaseSortColumn> | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly CaseColumn[]>();
  readonly columnsReorder = output<readonly CaseColumn[]>();
  readonly openRequested = output<string>();
  readonly machineLookupRequested = output<LookupRequest>();

  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  protected readonly CaseColumn = CaseColumn;
  protected readonly allColumns = Object.values(CaseColumn);
  protected readonly columnLabelKeys = COLUMN_LABEL_KEYS;
  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly aiUnavailableKey = AI_UNAVAILABLE_KEY;
  protected readonly openTooltipKey = OPEN_TOOLTIP_KEY;
  protected readonly labelChips = labelChipsOf;
  protected readonly showMachinesLoading = delayedLoading(this.isMachinesLoading);

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      ...Object.values(STATE_LABEL_KEYS),
      ...Object.values(SEVERITY_LABEL_KEYS),
      ...Object.values(RUN_STATE_LABEL_KEYS),
      UNKNOWN_MACHINE_KEY,
      AI_UNAVAILABLE_KEY,
    ]),
    { initialValue: {} as Record<string, string> },
  );

  private readonly _table = viewChild(Table);

  protected readonly grid = createGridTable<CaseListItem, CaseColumn, CaseFilter, CaseSortColumn>({
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
    table: this._table,
    textColumns: TEXT_COLUMNS,
    columnsChange: (columns) => this.columnsChange.emit(columns),
    columnsReorder: (columns) => this.columnsReorder.emit(columns),
  });

  protected readonly stateOptions = computed(() =>
    Object.values(CaseState).map((state) => ({ value: state, label: this._text(STATE_LABEL_KEYS[state]) })),
  );
  protected readonly severityOptions = computed(() =>
    Object.values(SignalSeverity).map((severity) => ({
      value: severity,
      label: this._text(SEVERITY_LABEL_KEYS[severity]),
      severity: this.severityTag(severity),
    })),
  );
  protected readonly aiOptions = computed(() => [
    ...Object.values(AgentRunState).map((state) => ({
      value: state,
      label: this._text(RUN_STATE_LABEL_KEYS[state]),
      severity: 'secondary' as const,
    })),
    { value: AI_UNAVAILABLE_OPTION, label: this._text(AI_UNAVAILABLE_KEY), severity: 'warn' as const },
  ]);
  protected readonly tableStyle = computed(() => ({
    'min-width': '48rem',
    ...(this.grid.tableRows().length > 0 ? {} : { height: '100%' }),
  }));

  protected machineChips(machineIds: readonly string[]): Chip[] {
    return lookupChips(machineIds, this.machines());
  }

  protected isFilterable(column: CaseColumn): boolean {
    return FILTERABLE_COLUMNS.includes(column);
  }

  protected machineLabel(machineId: string): MachineLabel {
    const label = this.machineLabels().get(machineId);
    return machineLabelOf(label?.name, label?.isDeleted ?? false, this._text(UNKNOWN_MACHINE_KEY));
  }

  protected stateLabel(state: CaseState): string {
    return this._text(STATE_LABEL_KEYS[state]);
  }

  protected severityLabel(severity: SignalSeverity): string {
    return this._text(SEVERITY_LABEL_KEYS[severity]);
  }

  protected severityTag(severity: SignalSeverity): 'danger' | 'warn' {
    return severity === SignalSeverity.Alarm ? 'danger' : 'warn';
  }

  protected runStateLabel(state: AgentRunState): string {
    return this._text(RUN_STATE_LABEL_KEYS[state]);
  }

  private _text(key: string): string {
    return this._labels()[key] ?? key;
  }
}
