import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import type { Menu } from 'primeng/menu';

import { FileColumn, type FileFilter, type FileGridItem } from '@files/file';

import { FileGrid } from './file-grid';

const EMPTY_FILTER: FileFilter = {
  fileName: '',
  extensions: [],
  uploadedAt: { from: null, to: null },
  machine: { machineIds: [], isUntiedIncluded: false },
  ingest: { states: [], isNeverIncluded: false },
};

const FILE: FileGridItem = {
  id: 'file-1',
  fileName: 'manual',
  extension: 'pdf',
  contentType: 'application/pdf',
  sizeBytes: 10,
  description: null,
  uploadedAt: '2026-03-01T08:00:00Z',
  machineId: null,
  machineName: null,
  ingestState: null,
  canIngest: false,
  rowVersion: 1,
};

interface GridAccess {
  menuItems(): { label?: string; icon?: string; command?: (event: never) => void }[];
  openMenu(event: Event, file: FileGridItem, menu: Menu): void;
}

class FakeResizeObserver {
  readonly observe = vi.fn();
  readonly unobserve = vi.fn();
  readonly disconnect = vi.fn();
}

function setup(options: { canDelete?: boolean } = {}): {
  grid: GridAccess;
  edits: FileGridItem[];
} {
  vi.stubGlobal('ResizeObserver', FakeResizeObserver);
  TestBed.configureTestingModule({ providers: [provideTranslateService()] });
  const fixture = TestBed.createComponent(FileGrid);
  fixture.componentRef.setInput('rows', []);
  fixture.componentRef.setInput('totalCount', 0);
  fixture.componentRef.setInput('columns', Object.values(FileColumn));
  fixture.componentRef.setInput('offset', 0);
  fixture.componentRef.setInput('limit', 25);
  fixture.componentRef.setInput('sort', null);
  fixture.componentRef.setInput('filter', EMPTY_FILTER);
  fixture.componentRef.setInput('canDelete', options.canDelete ?? false);
  fixture.detectChanges();
  const edits: FileGridItem[] = [];
  fixture.componentInstance.editRequested.subscribe((file) => edits.push(file));
  return { grid: fixture.componentInstance as unknown as GridAccess, edits };
}

function openMenu(grid: GridAccess): void {
  grid.openMenu(new Event('click'), FILE, { toggle: vi.fn() } as unknown as Menu);
}

describe('FileGrid', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('menuItems_WithoutEditRight_HoldsShowDetails', () => {
    const { grid } = setup();

    openMenu(grid);

    expect(grid.menuItems().map((item) => item.label)).toEqual(['FILES.ACTIONS_MENU.DOWNLOAD', 'ACTIONS.SHOW_DETAILS']);
    expect(grid.menuItems()[1].icon).toBe('pi pi-eye');
  });

  it('menuItems_ShowDetailsItem_EmitsEditRequestedWithTheRow', () => {
    const { grid, edits } = setup();
    openMenu(grid);

    grid.menuItems()[1].command?.({} as never);

    expect(edits).toEqual([FILE]);
  });

  it('menuItems_WithDeleteRight_KeepsShowDetailsAfterDownloadAndBeforeDelete', () => {
    const { grid } = setup({ canDelete: true });

    openMenu(grid);

    expect(grid.menuItems().map((item) => item.icon)).toEqual(['pi pi-download', 'pi pi-eye', 'pi pi-trash']);
  });
});
