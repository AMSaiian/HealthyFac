import { Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { Menu } from 'primeng/menu';
import { Skeleton } from 'primeng/skeleton';
import { SortIcon, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';

import { type MachineLabel, machineLabelOf } from '@shared/lookups/lookup-options';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import type { GridSort } from '@shared/grid/requests/grid-request';
import { ColumnChooser } from '@shared/grid/table/column-chooser/column-chooser';
import { createGridTable } from '@shared/grid/table/grid-table';
import { createRowMenu } from '@shared/grid/table/row-menu';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import {
  type WorkOrderDraft,
  WorkOrderDraftColumn,
  type WorkOrderDraftFilter,
  type WorkOrderDraftSortColumn,
} from '@work-orders/drafts/work-order-draft';
import {
  APPLY_KEY,
  APPLY_TOOLTIP_KEY,
  COLUMN_LABEL_KEYS,
  DISMISS_KEY,
  DISMISS_TOOLTIP_KEY,
  FILTER_READERS,
  FILTER_WRITERS,
  SORTABLE_COLUMNS,
  UNKNOWN_MACHINE_KEY,
} from '@work-orders/drafts/work-order-drafts-grid/work-order-drafts-grid.utils';
import { CATEGORY_LABEL_KEYS, PRIORITY_LABEL_KEYS } from '@work-orders/list/work-order-grid/work-order-grid.utils';

@Component({
  imports: [
    ButtonDirective,
    ColumnChooser,
    EmptyState,
    LocalDateTimePipe,
    Menu,
    RouterLink,
    Skeleton,
    SortIcon,
    TableModule,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-work-order-drafts-grid',
  templateUrl: './work-order-drafts-grid.html',
})
export class WorkOrderDraftsGrid {
  readonly rows = input.required<readonly WorkOrderDraft[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
  readonly columns = input.required<readonly WorkOrderDraftColumn[]>();
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<GridSort<WorkOrderDraftSortColumn> | null>();
  readonly filter = input.required<WorkOrderDraftFilter>();
  readonly canApply = input(false);
  readonly canDismiss = input(false);

  readonly filterChange = output<Partial<WorkOrderDraftFilter>>();
  readonly sortChange = output<GridSort<WorkOrderDraftSortColumn> | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly columnsChange = output<readonly WorkOrderDraftColumn[]>();
  readonly columnsReorder = output<readonly WorkOrderDraftColumn[]>();
  readonly applyRequested = output<string>();
  readonly dismissRequested = output<WorkOrderDraft>();

  protected readonly WorkOrderDraftColumn = WorkOrderDraftColumn;
  protected readonly allColumns = Object.values(WorkOrderDraftColumn);
  protected readonly columnLabelKeys = COLUMN_LABEL_KEYS;
  protected readonly pageSizes = [...PAGE_SIZES];

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      ...Object.values(PRIORITY_LABEL_KEYS),
      ...Object.values(CATEGORY_LABEL_KEYS),
      APPLY_KEY,
      APPLY_TOOLTIP_KEY,
      DISMISS_KEY,
      DISMISS_TOOLTIP_KEY,
      UNKNOWN_MACHINE_KEY,
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly grid = createGridTable<
    WorkOrderDraft,
    WorkOrderDraftColumn,
    WorkOrderDraftFilter,
    WorkOrderDraftSortColumn
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

  protected readonly rowMenu = createRowMenu<WorkOrderDraft>();
  protected readonly hasRowActions = computed(() => this.canApply() || this.canDismiss());
  protected readonly tableStyle = computed(() => ({
    'min-width': '56rem',
    ...(this.grid.tableRows().length > 0 ? {} : { height: '100%' }),
  }));
  protected readonly menuItems = computed<MenuItem[]>(() => {
    const labels = this._labels();
    return [
      ...(this.canApply()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: APPLY_KEY,
                tooltipKey: APPLY_TOOLTIP_KEY,
                icon: 'pi pi-check',
                onSelect: (draft) => this.applyRequested.emit(draft.id),
              },
              labels,
            ),
          ]
        : []),
      ...(this.canDismiss()
        ? [
            this.rowMenu.menuItem(
              {
                labelKey: DISMISS_KEY,
                tooltipKey: DISMISS_TOOLTIP_KEY,
                icon: 'pi pi-times',
                onSelect: (draft) => this.dismissRequested.emit(draft),
              },
              labels,
            ),
          ]
        : []),
    ];
  });

  protected machineLabel(draft: WorkOrderDraft): MachineLabel {
    return machineLabelOf(draft.machineName, draft.isMachineDeleted, this._text(UNKNOWN_MACHINE_KEY));
  }

  protected priorityLabel(priority: WorkOrderDraft['priority']): string {
    return this._text(PRIORITY_LABEL_KEYS[priority]);
  }

  protected categoryLabel(category: WorkOrderDraft['category']): string {
    return this._text(CATEGORY_LABEL_KEYS[category]);
  }

  private _text(key: string): string {
    return this._labels()[key] ?? key;
  }
}
