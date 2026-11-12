import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { type Machine, MachineColumn, type MachineFilter, MachineState } from '@machines/machine';

import { MachineGrid } from './machine-grid';

const EMPTY_FILTER: MachineFilter = {
  name: '',
  serialNumber: '',
  gatewayId: '',
  states: [],
  createdAt: { from: null, to: null },
  updatedAt: { from: null, to: null },
  hasPendingDrafts: null,
};

const MACHINE: Machine = {
  id: 'machine-1',
  name: 'Lathe',
  serialNumber: 'ABC-1',
  gatewayId: 'lmnoabcdefghijkl',
  state: MachineState.Operational,
  description: null,
  createdAt: '2026-03-01T08:00:00Z',
  updatedAt: null,
  pendingDraftCount: 0,
};

interface GridAccess {
  menuItems(): { label?: string; icon?: string; command?: (event: never) => void }[];
  rowMenu: { activeRow: Machine | null };
}

interface SetupOptions {
  canWrite?: boolean;
  canReadStatuses?: boolean;
}

interface Setup {
  updates: string[];
  grid: GridAccess;
}

class FakeResizeObserver {
  readonly observe = vi.fn();
  readonly unobserve = vi.fn();
  readonly disconnect = vi.fn();
}

function setup(options: SetupOptions = {}): Setup {
  vi.stubGlobal('ResizeObserver', FakeResizeObserver);
  TestBed.configureTestingModule({ providers: [provideTranslateService()] });
  const fixture = TestBed.createComponent(MachineGrid);
  fixture.componentRef.setInput('rows', []);
  fixture.componentRef.setInput('totalCount', 0);
  fixture.componentRef.setInput('columns', Object.values(MachineColumn));
  fixture.componentRef.setInput('offset', 0);
  fixture.componentRef.setInput('limit', 25);
  fixture.componentRef.setInput('sort', null);
  fixture.componentRef.setInput('filter', EMPTY_FILTER);
  fixture.componentRef.setInput('canWrite', options.canWrite ?? false);
  fixture.componentRef.setInput('canReadStatuses', options.canReadStatuses ?? false);
  fixture.detectChanges();
  const updates: string[] = [];
  fixture.componentInstance.updateRequested.subscribe((id) => updates.push(id));
  return { updates, grid: fixture.componentInstance as unknown as GridAccess };
}

describe('MachineGrid', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('menuItems_WithStatusesPermission_HoldsStatusesItem', () => {
    const { grid } = setup({ canReadStatuses: true });

    expect(grid.menuItems().map((item) => item.icon)).toEqual(['pi pi-list']);
  });

  it('menuItems_WithoutStatusesPermission_HoldsNoStatusesItem', () => {
    const { grid } = setup();

    expect(grid.menuItems()).toEqual([]);
  });

  it('menuItems_WithWritePermission_OpenItemIsFirst', () => {
    const { grid } = setup({ canWrite: true, canReadStatuses: true });

    expect(grid.menuItems().map((item) => item.icon)).toEqual(['pi pi-external-link', 'pi pi-list']);
  });

  it('menuItems_OpenItemOfActiveRow_EmitsUpdateRequestedWithItsId', () => {
    const { grid, updates } = setup({ canWrite: true });
    grid.rowMenu.activeRow = MACHINE;

    grid.menuItems()[0].command?.({} as never);

    expect(updates).toEqual([MACHINE.id]);
  });
});
