import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { Menu } from 'primeng/menu';
import { Tooltip } from 'primeng/tooltip';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { ColumnFilter, SortIcon, type TableFilterEvent, TableModule, type TablePageEvent } from 'primeng/table';

import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { keepWhileLoading, skeletonRows } from '@shared/grid/table/grid-display';
import { toSortDirection, toSortOrder } from '@shared/grid/table/grid-sort-order';
import { asList, readTableFilters, toConstraint, toTableFilters } from '@shared/grid/table/table-filters';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import {
  DraftColumn,
  DraftKind,
  DraftSortColumn,
  type MachineDraftFilter,
  type MachineDraftGridItem,
  type MachineDraftSort,
} from '@machines/drafts/draft';
import { draftRowMenuItems } from '@machines/drafts/draft-row-menu';

const COLUMN_LABEL_KEYS: Readonly<Record<DraftColumn, string>> = {
  [DraftColumn.Kind]: 'DRAFTS.LIST.COLUMNS.KIND',
  [DraftColumn.Label]: 'DRAFTS.LIST.COLUMNS.LABEL',
  [DraftColumn.SourceLocation]: 'DRAFTS.LIST.COLUMNS.SOURCE_LOCATION',
  [DraftColumn.ProposedAt]: 'DRAFTS.LIST.COLUMNS.PROPOSED_AT',
};

const KIND_KEYS: Readonly<Record<DraftKind, string>> = {
  [DraftKind.Machine]: 'DRAFTS.KINDS.MACHINE',
  [DraftKind.Metric]: 'DRAFTS.KINDS.METRIC',
  [DraftKind.Status]: 'DRAFTS.KINDS.STATUS',
};

const READERS: Readonly<Record<DraftColumn, (value: unknown) => Partial<MachineDraftFilter>>> = {
  [DraftColumn.Kind]: (value) => ({ kinds: asList<DraftKind>(value) }),
  [DraftColumn.Label]: () => ({}),
  [DraftColumn.SourceLocation]: () => ({}),
  [DraftColumn.ProposedAt]: () => ({}),
};

const WRITERS: Readonly<Record<DraftColumn, (filter: MachineDraftFilter) => unknown>> = {
  [DraftColumn.Kind]: (filter) => (filter.kinds.length === 0 ? null : [...filter.kinds]),
  [DraftColumn.Label]: () => null,
  [DraftColumn.SourceLocation]: () => null,
  [DraftColumn.ProposedAt]: () => null,
};

@Component({
  imports: [
    ButtonDirective,
    ColumnFilter,
    EmptyState,
    FormsModule,
    LocalDateTimePipe,
    Menu,
    Select,
    Skeleton,
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
  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  readonly rows = input.required<readonly MachineDraftGridItem[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<MachineDraftSort | null>();
  readonly filter = input.required<MachineDraftFilter>();
  readonly canWrite = input(false);
  /** The reason that **Apply** is off for each draft ID. A draft without a reason applies as it is. */
  readonly reasons = input<Readonly<Record<string, string | null>>>({});
  readonly applyingIds = input<readonly string[]>([]);

  readonly applyRequested = output<MachineDraftGridItem>();
  readonly filterChange = output<Partial<MachineDraftFilter>>();
  readonly sortChange = output<MachineDraftSort | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly reviewRequested = output<MachineDraftGridItem>();
  readonly dismissRequested = output<MachineDraftGridItem>();

  protected readonly isBusy = computed(() => this.applyingIds().length > 0);
  protected readonly menuItems = signal<MenuItem[]>([]);
  protected readonly DraftColumn = DraftColumn;
  protected readonly columns = Object.values(DraftColumn);
  protected readonly pageSizes = [...PAGE_SIZES];
  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(this._translate.stream(Object.values(KIND_KEYS)), {
    initialValue: {} as Record<string, string>,
  });
  protected readonly kindOptions = computed(() =>
    Object.values(DraftKind).map((value) => ({
      value,
      label: (this._labels() as Record<string, string>)[KIND_KEYS[value]] ?? KIND_KEYS[value],
    })),
  );
  protected readonly sortOrder = computed(() => toSortOrder(this.sort()?.direction));
  protected readonly tableFilters = computed(() => toTableFilters(this.columns, WRITERS, this.filter()));
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly keptRows = keepWhileLoading(this.rows, this.isLoading);
  protected readonly tableRows = computed((): unknown[] =>
    this.showSkeleton() ? skeletonRows(this.limit()) : [...this.keptRows()],
  );
  protected readonly tableStyle = computed(() => ({
    'min-width': '40rem',
    ...(this.showSkeleton() || this.keptRows().length > 0 ? {} : { height: '100%' }),
  }));

  protected columnLabelKey(column: DraftColumn): string {
    return COLUMN_LABEL_KEYS[column];
  }

  protected kindKey(kind: DraftKind): string {
    return KIND_KEYS[kind];
  }

  protected isSortable(column: DraftColumn): boolean {
    return (Object.values(DraftSortColumn) as string[]).includes(column);
  }

  protected onFilterApplied(event: TableFilterEvent): void {
    this.filterChange.emit(readTableFilters(this.columns, READERS, event.filters));
  }

  protected onFilterHidden(table: { filters: Record<string, unknown> }, column: DraftColumn): void {
    table.filters = { ...table.filters, [column]: [toConstraint(WRITERS[column](this.filter()))] };
  }

  protected onPage(event: TablePageEvent): void {
    this.pageChange.emit({ offset: event.first, limit: event.rows });
  }

  protected onSort(event: { field?: string | null; order?: number | null }): void {
    this.sortChange.emit(
      this.isSortable(event.field as DraftColumn)
        ? { column: event.field as DraftSortColumn, direction: toSortDirection(event.order) }
        : null,
    );
  }

  /** Builds the menu of the row at the click, so the texts follow the current language. */
  protected openMenu(event: Event, draft: MachineDraftGridItem, menu: Menu): void {
    this.menuItems.set(
      draftRowMenuItems({
        translate: (key) => this._translate.instant(key),
        isReviewDisabled: false,
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
