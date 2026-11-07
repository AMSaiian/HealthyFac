import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import type { TableFilterEvent } from 'primeng/table';

import {
  StatusDefinitionColumn,
  type StatusDefinitionFilter,
  StatusDefinitionSortColumn,
} from '@machines/statuses/status-definition';
import { SortDirection } from '@shared/grid/requests/grid-request';

import { StatusDefinitionGrid } from './status-definition-grid';

const EMPTY_FILTER: StatusDefinitionFilter = {
  code: '',
  severities: [],
  createdAt: { from: null, to: null },
  updatedAt: { from: null, to: null },
};

interface GridAccess {
  onFilterApplied(event: TableFilterEvent): void;
  onSort(event: { field?: string | null; order?: number | null }): void;
  isSortable(column: StatusDefinitionColumn): boolean;
  menuItems(): { label?: string }[];
}

class FakeResizeObserver {
  readonly observe = vi.fn();
  readonly unobserve = vi.fn();
  readonly disconnect = vi.fn();
}

interface Setup {
  emittedFilters: Partial<StatusDefinitionFilter>[];
  emittedSorts: unknown[];
  grid: GridAccess;
}

function setup(options: { canWrite?: boolean; canDelete?: boolean } = {}): Setup {
  vi.stubGlobal('ResizeObserver', FakeResizeObserver);
  TestBed.configureTestingModule({ providers: [provideTranslateService()] });
  const fixture = TestBed.createComponent(StatusDefinitionGrid);
  fixture.componentRef.setInput('rows', []);
  fixture.componentRef.setInput('totalCount', 0);
  fixture.componentRef.setInput('columns', Object.values(StatusDefinitionColumn));
  fixture.componentRef.setInput('offset', 0);
  fixture.componentRef.setInput('limit', 25);
  fixture.componentRef.setInput('sort', null);
  fixture.componentRef.setInput('filter', EMPTY_FILTER);
  fixture.componentRef.setInput('canWrite', options.canWrite ?? false);
  fixture.componentRef.setInput('canDelete', options.canDelete ?? false);
  fixture.detectChanges();
  const emittedFilters: Partial<StatusDefinitionFilter>[] = [];
  const emittedSorts: unknown[] = [];
  fixture.componentInstance.filterChange.subscribe((patch) => emittedFilters.push(patch));
  fixture.componentInstance.sortChange.subscribe((sort) => emittedSorts.push(sort));
  return { emittedFilters, emittedSorts, grid: fixture.componentInstance as unknown as GridAccess };
}

describe('StatusDefinitionGrid', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('onFilterApplied_WithCodeValue_EmitsOneTrimmedFilter', () => {
    const { grid, emittedFilters } = setup();

    grid.onFilterApplied({
      filters: { [StatusDefinitionColumn.Code]: [{ value: '  e-1  ' }] },
      filteredValue: [],
    } as unknown as TableFilterEvent);

    expect(emittedFilters).toEqual([
      { code: 'e-1', severities: [], createdAt: { from: null, to: null }, updatedAt: { from: null, to: null } },
    ]);
  });

  it('onSort_WithSortableField_EmitsColumnAndDirection', () => {
    const { grid, emittedSorts } = setup();

    grid.onSort({ field: StatusDefinitionSortColumn.Code, order: -1 });

    expect(emittedSorts).toEqual([{ column: StatusDefinitionSortColumn.Code, direction: SortDirection.Descending }]);
  });

  it('onSort_WithUnknownField_EmitsNull', () => {
    const { grid, emittedSorts } = setup();

    grid.onSort({ field: 'unknown', order: 1 });

    expect(emittedSorts).toEqual([null]);
  });

  it('menuItems_WithWriteOnly_HoldsOneItem', () => {
    const { grid } = setup({ canWrite: true });

    expect(grid.menuItems()).toHaveLength(1);
  });

  it('menuItems_WithWriteAndDelete_HoldsTwoItems', () => {
    const { grid } = setup({ canWrite: true, canDelete: true });

    expect(grid.menuItems()).toHaveLength(2);
  });

  it('menuItems_WithoutPermissions_IsEmpty', () => {
    const { grid } = setup();

    expect(grid.menuItems()).toEqual([]);
  });
});
