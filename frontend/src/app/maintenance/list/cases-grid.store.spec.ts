import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';

import { LookupsApi } from '@shared/lookups/lookups-api';
import { RealtimeTopic } from '@shared/realtime/realtime';
import {
  createFakeRealtimeConnection,
  provideFakeRealtimeConnection,
} from '@shared/realtime/testing/fake-realtime-connection';
import { AuthStore } from '@shared/security/auth-store/auth.store';

import { CasesApi } from '@maintenance/cases-api';

import { CasesGridStore } from './cases-grid.store';

const EMPTY_PAGE = { items: [], totalCount: 0 };

describe('CasesGridStore', () => {
  const realtime = createFakeRealtimeConnection();
  const search = vi.fn();

  async function setup(): Promise<InstanceType<typeof CasesGridStore>> {
    TestBed.configureTestingModule({
      providers: [
        provideFakeRealtimeConnection(realtime),
        CasesGridStore,
        { provide: CasesApi, useValue: { search } },
        { provide: LookupsApi, useValue: { machines: () => of([]), machinesByIds: () => of([]) } },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: AuthStore, useValue: { hasPermission: () => true } },
      ],
    });
    const store = TestBed.inject(CasesGridStore);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    return store;
  }

  beforeEach(() => {
    search.mockReset().mockReturnValue(of(EMPTY_PAGE));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('hint_OfTheCasesTopic_ReloadsTheListOnceAfterOneSecond', async () => {
    await setup();
    vi.useFakeTimers();
    const callsBefore = search.mock.calls.length;

    realtime.hints$.next({ topic: RealtimeTopic.Cases, caseId: 'case-1' });
    realtime.hints$.next({ topic: RealtimeTopic.Cases, caseId: 'case-2' });
    await vi.advanceTimersByTimeAsync(999);
    expect(search.mock.calls.length).toBe(callsBefore);
    await vi.advanceTimersByTimeAsync(1);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    vi.useRealTimers();

    expect(search.mock.calls.length - callsBefore).toBe(1);
  });

  it('hint_OfAnotherTopic_DoesNotReloadTheList', async () => {
    await setup();
    vi.useFakeTimers();
    const callsBefore = search.mock.calls.length;

    realtime.hints$.next({ topic: RealtimeTopic.Machines });
    await vi.advanceTimersByTimeAsync(5000);
    TestBed.tick();
    vi.useRealTimers();

    expect(search.mock.calls.length).toBe(callsBefore);
  });
});
