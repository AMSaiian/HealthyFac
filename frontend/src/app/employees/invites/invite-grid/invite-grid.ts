import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Menu } from 'primeng/menu';
import { Skeleton } from 'primeng/skeleton';
import { ColumnFilter, SortIcon, type TableFilterEvent, TableModule, type TablePageEvent } from 'primeng/table';

import { EmployeeRole } from '@shared/employee/employee';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { keepWhileLoading, skeletonRows } from '@shared/grid/table/grid-display';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { toSortDirection, toSortOrder } from '@shared/grid/table/grid-sort-order';
import { asText, readTableFilters, toTableFilters } from '@shared/grid/table/table-filters';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';
import { SQUARE_FILTER_BUTTON_PROPS } from '@shared/theme/square-button-props';

import {
  InviteColumn,
  type InviteFilter,
  type InviteRow,
  type InviteSort,
  InviteSortColumn,
} from '@employees/employee';
import { INVITE_MENU_KEYS, inviteMenuItems } from './invite-grid.utils';

const COLUMN_LABEL_KEYS: Readonly<Record<InviteColumn, string>> = {
  [InviteColumn.Email]: 'EMPLOYEES.COLUMNS.EMAIL',
  [InviteColumn.Role]: 'EMPLOYEES.COLUMNS.ROLE',
  [InviteColumn.ExpiresAt]: 'EMPLOYEES.COLUMNS.EXPIRES_AT',
  [InviteColumn.IsExpired]: 'EMPLOYEES.COLUMNS.IS_EXPIRED',
};
const ROLE_LABEL_KEYS: Readonly<Record<EmployeeRole, string>> = {
  [EmployeeRole.Worker]: 'EMPLOYEES.ROLES.WORKER',
  [EmployeeRole.Supervisor]: 'EMPLOYEES.ROLES.SUPERVISOR',
};
const SORTABLE_COLUMNS: readonly string[] = Object.values(InviteSortColumn);
const FILTER_COLUMNS: readonly InviteColumn.Email[] = [InviteColumn.Email];
const FILTER_READERS = { [InviteColumn.Email]: (value: unknown): Partial<InviteFilter> => ({ email: asText(value) }) };
const FILTER_WRITERS = { [InviteColumn.Email]: (filter: InviteFilter): unknown => filter.email || null };

@Component({
  imports: [
    ButtonDirective,
    ColumnFilter,
    EmptyState,
    FormsModule,
    InputText,
    LocalDateTimePipe,
    Menu,
    Skeleton,
    SortIcon,
    TableModule,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-invite-grid',
  templateUrl: './invite-grid.html',
})
export class InviteGrid {
  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  readonly rows = input.required<readonly InviteRow[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<InviteSort | null>();
  readonly filter = input.required<InviteFilter>();

  readonly filterChange = output<Partial<InviteFilter>>();
  readonly sortChange = output<InviteSort | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly resendRequested = output<string>();
  readonly revokeRequested = output<InviteRow>();

  protected readonly InviteColumn = InviteColumn;
  protected readonly columns = Object.values(InviteColumn);
  protected readonly pageSizes = [...PAGE_SIZES];

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([...Object.values(ROLE_LABEL_KEYS), ...Object.values(INVITE_MENU_KEYS)]),
    { initialValue: {} as Record<string, string> },
  );
  private readonly _activeRow = signal<InviteRow | null>(null);

  protected readonly sortOrder = computed(() => toSortOrder(this.sort()?.direction));
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly keptRows = keepWhileLoading(this.rows, this.isLoading);
  protected readonly skeletonRows = computed(() => skeletonRows(this.limit()));
  protected readonly tableFilters = computed(() => toTableFilters(FILTER_COLUMNS, FILTER_WRITERS, this.filter()));
  protected readonly tableRows = computed((): unknown[] =>
    this.showSkeleton() ? this.skeletonRows() : [...this.keptRows()],
  );
  protected readonly tableStyle = computed(() => ({
    'min-width': '48rem',
    ...(this.showSkeleton() || this.keptRows().length > 0 ? {} : { height: '100%' }),
  }));

  protected readonly menuItems = computed<MenuItem[]>(() =>
    inviteMenuItems(
      this._labels(),
      () => this._withActiveRow((row) => this.resendRequested.emit(row.id)),
      () => this._withActiveRow((row) => this.revokeRequested.emit(row)),
    ),
  );

  protected columnLabelKey(column: InviteColumn): string {
    return COLUMN_LABEL_KEYS[column];
  }

  protected isSortable(column: InviteColumn): boolean {
    return SORTABLE_COLUMNS.includes(column);
  }

  protected roleLabel(role: EmployeeRole): string {
    return this._labels()[ROLE_LABEL_KEYS[role]] ?? role;
  }

  protected onFilterApplied(event: TableFilterEvent): void {
    this.filterChange.emit(readTableFilters(FILTER_COLUMNS, FILTER_READERS, event.filters));
  }

  protected onPage(event: TablePageEvent): void {
    this.pageChange.emit({ offset: event.first, limit: event.rows });
  }

  protected onSort(event: { field?: string | null; order?: number | null }): void {
    const column = SORTABLE_COLUMNS.find((candidate) => candidate === event.field) as InviteSortColumn | undefined;
    this.sortChange.emit(column === undefined ? null : { column, direction: toSortDirection(event.order) });
  }

  protected openMenu(event: Event, row: InviteRow, menu: Menu): void {
    this._activeRow.set(row);
    menu.toggle(event);
  }

  private _withActiveRow(action: (row: InviteRow) => void): void {
    const row = this._activeRow();
    if (row !== null) {
      action(row);
    }
  }
}
