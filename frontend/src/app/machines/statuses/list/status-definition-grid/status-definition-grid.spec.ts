import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

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
  grid: { onSort(event: { field?: string | null; order?: number | null }): void };
  menuItems(): { label?: string }[];
}

class FakeResizeObserver {
  readonly observe = vi.fn();
  readonly unobserve = vi.fn();
  readonly disconnect = vi.fn();
}

interface Setup {
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
  const emittedSorts: unknown[] = [];
  fixture.componentInstance.sortChange.subscribe((sort) => emittedSorts.push(sort));
  return { emittedSorts, grid: fixture.componentInstance as unknown as GridAccess };
}

describe('StatusDefinitionGrid', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('onSort_WithSortableField_EmitsColumnAndDirection', () => {
    const { grid, emittedSorts } = setup();

    grid.grid.onSort({ field: StatusDefinitionSortColumn.Code, order: -1 });

    expect(emittedSorts).toEqual([{ column: StatusDefinitionSortColumn.Code, direction: SortDirection.Descending }]);
  });

  it('onSort_WithUnknownField_EmitsNull', () => {
    const { grid, emittedSorts } = setup();

    grid.grid.onSort({ field: 'unknown', order: 1 });

    expect(emittedSorts).toEqual([null]);
  });

  it.each([
    { name: 'WithWriteOnly_HoldsOneItem', canWrite: true, canDelete: false, itemCount: 1 },
    { name: 'WithWriteAndDelete_HoldsTwoItems', canWrite: true, canDelete: true, itemCount: 2 },
    { name: 'WithoutPermissions_IsEmpty', canWrite: false, canDelete: false, itemCount: 0 },
  ])('menuItems_$name', ({ canWrite, canDelete, itemCount }) => {
    const { grid } = setup({ canWrite, canDelete });

    expect(grid.menuItems()).toHaveLength(itemCount);
  });
});
