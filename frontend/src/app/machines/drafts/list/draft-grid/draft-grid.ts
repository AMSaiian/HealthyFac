import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Menu } from 'primeng/menu';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { ColumnFilter, SortIcon, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';

import { DEFAULT_PAGE_SIZE, PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { ChipSummary } from '@shared/chips/chip-summary/chip-summary';
import { labelChipsOf } from '@shared/chips/chip-summary/chip-summary.utils';
import { createGridTable } from '@shared/grid/table/grid-table';
import { asList } from '@shared/grid/table/table-filters';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import {
  DraftColumn,
  DraftKind,
  DraftSortColumn,
  DraftState,
  type MachineDraftFilter,
  type MachineDraftGridItem,
  type MachineDraftSort,
} from '@machines/drafts/draft';
import { draftRowMenuItems } from '@machines/drafts/draft-row-menu';
import { isReviewDisabledOf, machineTitleOf } from '@machines/drafts/list/draft-grid/draft-grid.utils';

const COLUMN_LABEL_KEYS: Readonly<Record<DraftColumn, string>> = {
  [DraftColumn.Kind]: 'DRAFTS.LIST.COLUMNS.KIND',
  [DraftColumn.State]: 'DRAFTS.LIST.COLUMNS.STATE',
  [DraftColumn.Machine]: 'DRAFTS.LIST.COLUMNS.MACHINE',
  [DraftColumn.Label]: 'DRAFTS.LIST.COLUMNS.LABEL',
  [DraftColumn.SourceLocation]: 'DRAFTS.LIST.COLUMNS.SOURCE_LOCATION',
  [DraftColumn.ProposedAt]: 'DRAFTS.LIST.COLUMNS.PROPOSED_AT',
};

const KIND_KEYS: Readonly<Record<DraftKind, string>> = {
  [DraftKind.Machine]: 'DRAFTS.KINDS.MACHINE',
  [DraftKind.Metric]: 'DRAFTS.KINDS.METRIC',
  [DraftKind.Status]: 'DRAFTS.KINDS.STATUS',
};

const STATE_KEYS: Readonly<Record<DraftState, string>> = {
  [DraftState.Pending]: 'DRAFTS.STATES.PENDING',
  [DraftState.Applied]: 'DRAFTS.STATES.APPLIED',
  [DraftState.Dismissed]: 'DRAFTS.STATES.DISMISSED',
};

const STATE_SEVERITIES: Readonly<Record<DraftState, 'info' | 'success' | 'secondary'>> = {
  [DraftState.Pending]: 'info',
  [DraftState.Applied]: 'success',
  [DraftState.Dismissed]: 'secondary',
};

const READERS: Readonly<Record<DraftColumn, (value: unknown) => Partial<MachineDraftFilter>>> = {
  [DraftColumn.Kind]: (value) => ({ kinds: asList<DraftKind>(value) }),
  [DraftColumn.State]: (value) => ({ states: asList<DraftState>(value) }),
  [DraftColumn.Machine]: () => ({}),
  [DraftColumn.Label]: () => ({}),
  [DraftColumn.SourceLocation]: () => ({}),
  [DraftColumn.ProposedAt]: () => ({}),
};

const WRITERS: Readonly<Record<DraftColumn, (filter: MachineDraftFilter) => unknown>> = {
  [DraftColumn.Kind]: (filter) => (filter.kinds.length === 0 ? null : [...filter.kinds]),
  [DraftColumn.State]: (filter) => (filter.states.length === 0 ? null : [...filter.states]),
  [DraftColumn.Machine]: () => null,
  [DraftColumn.Label]: () => null,
  [DraftColumn.SourceLocation]: () => null,
  [DraftColumn.ProposedAt]: () => null,
};

@Component({
  imports: [
    ChipSummary,
    ButtonDirective,
    Checkbox,
    ColumnFilter,
    EmptyState,
    FormsModule,
    LocalDateTimePipe,
    Menu,
    Select,
    Skeleton,
    Tag,
    SortIcon,
    TableModule,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-draft-grid',
  templateUrl: './draft-grid.html',
})
export class DraftGrid {
  readonly rows = input.required<readonly MachineDraftGridItem[]>();
  readonly totalCount = input(0);
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
  readonly offset = input(0);
  readonly limit = input(DEFAULT_PAGE_SIZE);
  readonly sort = input<MachineDraftSort | null>(null);
  readonly filter = input<MachineDraftFilter>({ kinds: [], states: [] });
  /** The ingest summary lists the drafts of several machines, so each row names its machine. */
  readonly isMachineShown = input(false);
  readonly canWrite = input(false);
  /** The reason that **Apply** is off for each draft ID. A draft without a reason applies as it is. */
  readonly reasons = input<Readonly<Record<string, string | null>>>({});
  readonly applyingIds = input<readonly string[]>([]);
  /** The IDs of the checked drafts, on every page. */
  readonly selectedIds = input<readonly string[]>([]);

  readonly applyRequested = output<MachineDraftGridItem>();
  readonly filterChange = output<Partial<MachineDraftFilter>>();
  readonly sortChange = output<MachineDraftSort | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly reviewRequested = output<MachineDraftGridItem>();
  readonly dismissRequested = output<MachineDraftGridItem>();
  readonly selectionToggled = output<{ item: MachineDraftGridItem; isChecked: boolean }>();
  /** The header check: check or uncheck every pending draft of the page. */
  readonly pageSelectionToggled = output<boolean>();

  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  protected readonly isBusy = computed(() => this.applyingIds().length > 0);
  protected readonly showBusy = delayedLoading(this.isBusy);
  protected readonly menuItems = signal<MenuItem[]>([]);
  protected readonly DraftColumn = DraftColumn;
  protected readonly allColumns = Object.values(DraftColumn);
  protected readonly visibleColumns = computed(() =>
    this.allColumns.filter((column) => column !== DraftColumn.Machine || this.isMachineShown()),
  );
  protected readonly pageSizes = [...PAGE_SIZES];
  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([...Object.values(KIND_KEYS), ...Object.values(STATE_KEYS)]),
    {
      initialValue: {} as Record<string, string>,
    },
  );

  protected readonly grid = createGridTable<MachineDraftGridItem, DraftColumn, MachineDraftFilter, DraftSortColumn>({
    allColumns: this.allColumns,
    columns: signal(this.allColumns),
    filter: this.filter,
    sort: this.sort,
    limit: this.limit,
    isLoading: this.isLoading,
    hasLoaded: this.hasLoaded,
    rows: this.rows,
    filterReaders: READERS,
    filterWriters: WRITERS,
    sortableColumns: Object.values(DraftSortColumn),
    labelKeys: COLUMN_LABEL_KEYS,
    filterChange: (patch) => this.filterChange.emit(patch),
    sortChange: (sort) => this.sortChange.emit(sort),
    pageChange: (page) => this.pageChange.emit(page),
    columnsChange: () => undefined,
    columnsReorder: () => undefined,
  });

  protected readonly kindOptions = computed(() =>
    Object.values(DraftKind).map((value) => ({
      value,
      label: (this._labels() as Record<string, string>)[KIND_KEYS[value]] ?? KIND_KEYS[value],
    })),
  );
  protected readonly stateOptions = computed(() =>
    Object.values(DraftState).map((value) => ({
      value,
      label: (this._labels() as Record<string, string>)[STATE_KEYS[value]] ?? STATE_KEYS[value],
      severity: STATE_SEVERITIES[value],
    })),
  );
  protected readonly labelChips = labelChipsOf;
  protected readonly tableStyle = computed(() => ({
    'min-width': '40rem',
    ...(this.grid.tableRows().length > 0 ? {} : { height: '100%' }),
  }));

  protected readonly stateKey = (state: DraftState): string => STATE_KEYS[state];
  protected readonly stateSeverity = (state: DraftState): 'info' | 'success' | 'secondary' => STATE_SEVERITIES[state];
  protected readonly isPending = (row: MachineDraftGridItem): boolean => row.state === DraftState.Pending;
  protected readonly isSelected = (row: MachineDraftGridItem): boolean => this.selectedIds().includes(row.id);
  protected readonly hasPendingRows = computed(() => this._pendingRows().length > 0);
  private readonly _pendingRows = computed(() => this.rows().filter(this.isPending));
  protected readonly isPageChecked = computed(
    () => this._pendingRows().length > 0 && this._pendingRows().every(this.isSelected),
  );
  protected readonly isPageIndeterminate = computed(
    () => !this.isPageChecked() && this._pendingRows().some(this.isSelected),
  );
  protected readonly machineTitleOf = machineTitleOf;

  protected kindKey(kind: DraftKind): string {
    return KIND_KEYS[kind];
  }

  /** Builds the menu of the row at the click, so the texts follow the current language. */
  protected openMenu(event: Event, draft: MachineDraftGridItem, menu: Menu): void {
    this.menuItems.set(
      draftRowMenuItems({
        translate: (key) => this._translate.instant(key),
        isReviewDisabled: isReviewDisabledOf(draft),
        applyReasonKey: this.reasons()[draft.id],
        isBusy: this.isBusy(),
        onReview: () => this.reviewRequested.emit(draft),
        onApply: () => this.applyRequested.emit(draft),
        onDismiss: () => this.dismissRequested.emit(draft),
      }),
    );
    menu.toggle(event);
  }
}
