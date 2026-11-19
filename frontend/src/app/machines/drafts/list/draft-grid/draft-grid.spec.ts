import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { LanguageStore } from '@shared/i18n/language.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { DraftKind, DraftState, type MachineDraftGridItem } from '@machines/drafts/draft';

import { DraftGrid } from './draft-grid';

const DRAFT: MachineDraftGridItem = {
  id: 'd1',
  kind: DraftKind.Metric,
  state: DraftState.Pending,
  attemptId: 'a1',
  fileId: 'f1',
  proposedAt: '2026-03-01T08:00:00Z',
  sourceLocation: null,
  label: 'Spindle speed',
  rowVersion: 1,
  isCreate: false,
  machineId: 'm1',
  machineName: 'Lathe',
};

interface GridAccess {
  menuItems(): { label?: string; disabled?: boolean }[];
  openMenu(event: Event, draft: MachineDraftGridItem, menu: { toggle: () => void }): void;
}

class FakeResizeObserver {
  readonly observe = vi.fn();
  readonly unobserve = vi.fn();
  readonly disconnect = vi.fn();
}

function setup(inputs: { isBusy: boolean; applyingIds: readonly string[] }): GridAccess {
  vi.stubGlobal('ResizeObserver', FakeResizeObserver);
  TestBed.configureTestingModule({
    providers: [
      provideTranslateService(),
      { provide: LanguageStore, useValue: { language: () => 'en' } },
      {
        provide: CLOCK,
        useValue: { now: () => Temporal.Instant.from('2027-06-01T00:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
      },
    ],
  });
  const fixture = TestBed.createComponent(DraftGrid);
  fixture.componentRef.setInput('rows', [DRAFT]);
  fixture.componentRef.setInput('isBusy', inputs.isBusy);
  fixture.componentRef.setInput('applyingIds', inputs.applyingIds);
  fixture.detectChanges();
  return fixture.componentInstance as unknown as GridAccess;
}

describe('DraftGrid', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('openMenu_WhenARunIsInProgressWithoutRowIds_DisablesApplyAndDismiss', () => {
    const grid = setup({ isBusy: true, applyingIds: [] });

    grid.openMenu(new Event('click'), DRAFT, { toggle: vi.fn() });

    const disabled = grid.menuItems().map((item) => item.disabled);
    expect(disabled.slice(1)).toEqual([true, true]);
  });

  it('openMenu_WhenNoRunIsInProgress_KeepsApplyAndDismissEnabled', () => {
    const grid = setup({ isBusy: false, applyingIds: [] });

    grid.openMenu(new Event('click'), DRAFT, { toggle: vi.fn() });

    const disabled = grid.menuItems().map((item) => item.disabled);
    expect(disabled.slice(1)).toEqual([false, false]);
  });
});
