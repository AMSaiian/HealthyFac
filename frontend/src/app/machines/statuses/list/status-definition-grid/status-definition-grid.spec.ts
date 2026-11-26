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

describe('StatusDefinitionGrid', { tags: ['SignalModel'] }, () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it.each([
    [
      'SortableField_EmitsColumnAndDirection',
      { field: StatusDefinitionSortColumn.Code, order: -1 },
      [{ column: StatusDefinitionSortColumn.Code, direction: SortDirection.Descending }],
    ],
    ['UnknownField_EmitsNull', { field: 'unknown', order: 1 }, [null]],
  ])('onSort_With%s', (_name, event, expected) => {
    const { grid, emittedSorts } = setup();

    grid.grid.onSort(event);

    expect(emittedSorts).toEqual(expected);
  });

  it.each([
    { name: 'WithWriteOnly_HoldsShowDetailsOnly', canWrite: true, canDelete: false, itemCount: 1 },
    { name: 'WithWriteAndDelete_HoldsShowDetailsAndDelete', canWrite: true, canDelete: true, itemCount: 2 },
    { name: 'WithoutPermissions_HoldsShowDetailsOnly', canWrite: false, canDelete: false, itemCount: 1 },
  ])('menuItems_$name', ({ canWrite, canDelete, itemCount }) => {
    const { grid } = setup({ canWrite, canDelete });

    expect(grid.menuItems()).toHaveLength(itemCount);
  });

  it('menuItems_WithoutWritePermission_HoldsShowDetails', () => {
    const { grid } = setup();

    expect(grid.menuItems().map((item) => item.label)).toEqual(['ACTIONS.SHOW_DETAILS']);
  });
});
