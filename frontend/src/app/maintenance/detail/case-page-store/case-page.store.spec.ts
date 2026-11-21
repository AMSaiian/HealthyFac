import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { firstValueFrom, of } from 'rxjs';

import { LookupsApi } from '@shared/lookups/lookups-api';
import { RealtimeTopic } from '@shared/realtime/realtime';
import {
  createFakeRealtimeConnection,
  provideFakeRealtimeConnection,
} from '@shared/realtime/testing/fake-realtime-connection';
import { AuthStore } from '@shared/security/auth-store/auth.store';

import type { CaseDetail } from '@maintenance/case';
import { CasesApi } from '@maintenance/cases-api';

import { CasePageStore } from './case-page.store';

const EMPTY_PAGE = { items: [], totalCount: 0 };
const CASE_ID = 'case-1';

describe('CasePageStore', () => {
  const realtime = createFakeRealtimeConnection();
  const get = vi.fn();
  const searchSignals = vi.fn();
  const searchRuns = vi.fn();

  async function setup(): Promise<InstanceType<typeof CasePageStore>> {
    TestBed.configureTestingModule({
      providers: [
        provideFakeRealtimeConnection(realtime),
        CasePageStore,
        { provide: CasesApi, useValue: { get, searchSignals, searchRuns } },
        { provide: LookupsApi, useValue: { machinesByIds: () => of([]) } },
        { provide: AuthStore, useValue: { hasPermission: () => true } },
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    const store = TestBed.inject(CasePageStore);
    await firstValueFrom(store.openCase(CASE_ID));
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    return store;
  }

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  beforeEach(() => {
    get.mockReset().mockReturnValue(of({ id: CASE_ID, machineId: 'machine-1' } as CaseDetail));
    searchSignals.mockReset().mockReturnValue(of(EMPTY_PAGE));
    searchRuns.mockReset().mockReturnValue(of(EMPTY_PAGE));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('hint_WithTheCaseOfThePage_ReloadsTheHeaderAndBothTabsOnceAfterOneSecond', async () => {
    const store = await setup();
    vi.useFakeTimers();
    const before = {
      get: get.mock.calls.length,
      signals: searchSignals.mock.calls.length,
      runs: searchRuns.mock.calls.length,
    };

    realtime.hints$.next({ topic: RealtimeTopic.Cases, caseId: CASE_ID });
    realtime.hints$.next({ topic: RealtimeTopic.Cases, caseId: CASE_ID });
    await vi.advanceTimersByTimeAsync(999);
    expect(get.mock.calls.length).toBe(before.get);
    await vi.advanceTimersByTimeAsync(1);
    await settle();
    vi.useRealTimers();

    expect(get.mock.calls.length - before.get).toBe(1);
    expect(searchSignals.mock.calls.length - before.signals).toBe(1);
    expect(searchRuns.mock.calls.length - before.runs).toBe(1);
    expect(store.refreshSecondsLeft()).toBe(0);
  });

  it('hint_WithAnotherCase_ReloadsNothing', async () => {
    await setup();
    vi.useFakeTimers();
    const callsBefore = get.mock.calls.length;

    realtime.hints$.next({ topic: RealtimeTopic.Cases, caseId: 'case-2' });
    await vi.advanceTimersByTimeAsync(5000);
    await settle();
    vi.useRealTimers();

    expect(get.mock.calls.length).toBe(callsBefore);
  });
});
