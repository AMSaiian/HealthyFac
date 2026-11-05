import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import type { TableFilterEvent } from 'primeng/table';

import { Temporal } from '@shared/time/temporal';
import { MachineColumn, type MachineFilter, MachineState } from '@machines/machine';

import { MachineGrid } from './machine-grid';

const EMPTY_FILTER: MachineFilter = {
  name: '',
  serialNumber: '',
  departmentIds: [],
  states: [],
  createdAt: { from: null, to: null },
  updatedAt: { from: null, to: null },
};

interface GridAccess {
  onFilterApplied(event: TableFilterEvent): void;
  onFilterHidden(column: MachineColumn): void;
  table: () => { filters: Record<string, { value: unknown }[]> };
}

class FakeResizeObserver {
  readonly observe = vi.fn();
  readonly unobserve = vi.fn();
  readonly disconnect = vi.fn();
}

interface Setup {
  emitted: Partial<MachineFilter>[];
  grid: GridAccess;
}

function setup(filter: MachineFilter = EMPTY_FILTER): Setup {
  vi.stubGlobal('ResizeObserver', FakeResizeObserver);
  TestBed.configureTestingModule({ providers: [provideTranslateService()] });
  const fixture = TestBed.createComponent(MachineGrid);
  fixture.componentRef.setInput('rows', []);
  fixture.componentRef.setInput('totalCount', 0);
  fixture.componentRef.setInput('columns', Object.values(MachineColumn));
  fixture.componentRef.setInput('offset', 0);
  fixture.componentRef.setInput('limit', 25);
  fixture.componentRef.setInput('sort', null);
  fixture.componentRef.setInput('filter', filter);
  fixture.componentRef.setInput('departments', []);
  fixture.detectChanges();
  const emitted: Partial<MachineFilter>[] = [];
  fixture.componentInstance.filterChange.subscribe((patch) => emitted.push(patch));
  return { emitted, grid: fixture.componentInstance as unknown as GridAccess };
}

function appliedEvent(filters: Record<string, { value: unknown }[]>): TableFilterEvent {
  return { filters, filteredValue: [] } as unknown as TableFilterEvent;
}

describe('MachineGrid', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('onFilterApplied_WithAllColumns_EmitsOneTrimmedFilter', () => {
    const { grid, emitted } = setup();

    grid.onFilterApplied(
      appliedEvent({
        [MachineColumn.Name]: [{ value: '  ab  ' }],
        [MachineColumn.State]: [{ value: [MachineState.Operational] }],
      }),
    );

    expect(emitted).toEqual([
      {
        name: 'ab',
        serialNumber: '',
        departmentIds: [],
        states: [MachineState.Operational],
        createdAt: { from: null, to: null },
        updatedAt: { from: null, to: null },
      },
    ]);
  });

  it('onFilterApplied_WithClearedColumn_EmitsEmptyValue', () => {
    const day = Temporal.PlainDate.from('2026-03-29');
    const { grid, emitted } = setup({ ...EMPTY_FILTER, createdAt: { from: day, to: day } });

    grid.onFilterApplied(appliedEvent({ [MachineColumn.CreatedAt]: [{ value: null }] }));

    expect(emitted).toHaveLength(1);
    expect(emitted[0].createdAt).toEqual({ from: null, to: null });
  });

  it('onFilterHidden_WithPendingValue_RestoresAppliedValue', () => {
    const { grid } = setup({ ...EMPTY_FILTER, name: 'applied' });
    grid.table().filters[MachineColumn.Name] = [{ value: 'pending' }];

    grid.onFilterHidden(MachineColumn.Name);

    expect(grid.table().filters[MachineColumn.Name][0].value).toBe('applied');
  });
});
