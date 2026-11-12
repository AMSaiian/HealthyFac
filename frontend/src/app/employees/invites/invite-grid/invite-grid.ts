import { Component, computed, inject, input, output, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { MenuItem } from 'primeng/api';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Menu } from 'primeng/menu';
import { Skeleton } from 'primeng/skeleton';
import { ColumnFilter, SortIcon, TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';

import { EmployeeRole } from '@shared/employee/employee';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import type { GridSort } from '@shared/grid/requests/grid-request';
import { createGridTable } from '@shared/grid/table/grid-table';
import { createRowMenu } from '@shared/grid/table/row-menu';
import { asText } from '@shared/grid/table/table-filters';
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
const INVITE_COLUMNS: readonly InviteColumn[] = Object.values(InviteColumn);
const SORTABLE_COLUMNS: readonly string[] = Object.values(InviteSortColumn);
const FILTER_READERS: Readonly<Record<InviteColumn, (value: unknown) => Partial<InviteFilter>>> = {
  [InviteColumn.Email]: (value) => ({ email: asText(value) }),
  [InviteColumn.Role]: () => ({}),
  [InviteColumn.ExpiresAt]: () => ({}),
  [InviteColumn.IsExpired]: () => ({}),
};
const FILTER_WRITERS: Readonly<Record<InviteColumn, (filter: InviteFilter) => unknown>> = {
  [InviteColumn.Email]: (filter) => filter.email || null,
  [InviteColumn.Role]: () => null,
  [InviteColumn.ExpiresAt]: () => null,
  [InviteColumn.IsExpired]: () => null,
};

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
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-invite-grid',
  templateUrl: './invite-grid.html',
})
export class InviteGrid {
  readonly rows = input.required<readonly InviteRow[]>();
  readonly totalCount = input.required<number>();
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly sort = input.required<InviteSort | null>();
  readonly filter = input.required<InviteFilter>();

  readonly filterChange = output<Partial<InviteFilter>>();
  readonly sortChange = output<GridSort<InviteSortColumn> | null>();
  readonly pageChange = output<{ offset: number; limit: number }>();
  readonly resendRequested = output<string>();
  readonly revokeRequested = output<InviteRow>();

  protected readonly squareFilterButtonProps = SQUARE_FILTER_BUTTON_PROPS;
  protected readonly InviteColumn = InviteColumn;
  protected readonly columns = INVITE_COLUMNS;
  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly labelKeys = COLUMN_LABEL_KEYS;

  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([...Object.values(ROLE_LABEL_KEYS), ...Object.values(INVITE_MENU_KEYS)]),
    { initialValue: {} as Record<string, string> },
  );

  protected readonly grid = createGridTable<InviteRow, InviteColumn, InviteFilter, InviteSortColumn>({
    allColumns: INVITE_COLUMNS,
    columns: signal(INVITE_COLUMNS),
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
    columnsChange: () => undefined,
    columnsReorder: () => undefined,
  });

  protected readonly rowMenu = createRowMenu<InviteRow>();
  protected readonly menuItems = computed<MenuItem[]>(() =>
    inviteMenuItems(
      this.rowMenu,
      this._labels(),
      (row) => this.resendRequested.emit(row.id),
      (row) => this.revokeRequested.emit(row),
    ),
  );
  protected readonly tableStyle = computed(() => ({
    'min-width': '48rem',
    ...(this.grid.tableRows().length > 0 ? {} : { height: '100%' }),
  }));

  protected roleLabel(role: EmployeeRole): string {
    return this._labels()[ROLE_LABEL_KEYS[role]] ?? role;
  }
}
