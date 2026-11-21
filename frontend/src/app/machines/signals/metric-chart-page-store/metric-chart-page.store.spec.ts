import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom, of } from 'rxjs';

import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import type { Machine } from '@machines/machine';
import { MachinesApi } from '@machines/machines-api';
import { MetricDefinitionsApi } from '@machines/metrics/metric-definitions-api';
import { MetricValueKind } from '@machines/metrics/metric-definition';
import { METRIC_CHART_RELOAD_INTERVAL_MS } from '@machines/signals/metric-chart-page-store/metric-chart-page.store.utils';
import { ChartRange } from '@machines/signals/signal-episode';
import { SignalsApi } from '@machines/signals/signals-api';

import { MetricChartPageStore } from './metric-chart-page.store';

const MACHINE_ID = 'machine-1';
const FROM = '2027-06-01T08:00:00Z';
const TO = '2027-06-01T09:00:00Z';

describe('MetricChartPageStore', () => {
  const getHistory = vi.fn();
  const getEpisodes = vi.fn();
  const getPhaseTimes = vi.fn();
  const getStateTimes = vi.fn();

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(): Promise<InstanceType<typeof MetricChartPageStore>> {
    TestBed.configureTestingModule({
      providers: [
        MetricChartPageStore,
        { provide: SignalsApi, useValue: { getHistory, getEpisodes, getPhaseTimes } },
        {
          provide: MachinesApi,
          useValue: { get: () => of({ id: MACHINE_ID } as Machine), getStateTimes },
        },
        {
          provide: MetricDefinitionsApi,
          useValue: {
            getDefinitions: () =>
              of({
                revision: 1,
                definitions: [{ id: 'metric-1', key: 'temperature', valueKind: MetricValueKind.Numeric }],
              }),
          },
        },
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from(TO), timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(MetricChartPageStore);
    await firstValueFrom(store.openMachine(MACHINE_ID));
    store.setWidth(300);
    await settle();
    return store;
  }

  async function advance(milliseconds: number): Promise<void> {
    await vi.advanceTimersByTimeAsync(milliseconds);
    await settle();
  }

  beforeEach(() => {
    getHistory.mockReset().mockReturnValue(of({ buckets: [], from: FROM, to: TO }));
    getEpisodes.mockReset().mockReturnValue(of({ items: [] }));
    getPhaseTimes.mockReset().mockReturnValue(of({ items: [] }));
    getStateTimes.mockReset().mockReturnValue(of({ items: [] }));
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('history_WithAPresetRange_LoadsAgainAt15And30Seconds', async () => {
    await setup();
    const callsBefore = getHistory.mock.calls.length;

    await advance(METRIC_CHART_RELOAD_INTERVAL_MS - 1);
    expect(getHistory.mock.calls.length).toBe(callsBefore);
    await advance(1);
    expect(getHistory.mock.calls.length - callsBefore).toBe(1);
    await advance(METRIC_CHART_RELOAD_INTERVAL_MS);

    expect(getHistory.mock.calls.length - callsBefore).toBe(2);
  });

  it('history_WithACustomRange_DoesNotLoadAgain', async () => {
    const store = await setup();
    store.selectCustomRange(FROM, TO);
    await advance(1000);
    const callsBefore = getHistory.mock.calls.length;

    await advance(METRIC_CHART_RELOAD_INTERVAL_MS * 3);

    expect(getHistory.mock.calls.length).toBe(callsBefore);
  });

  it('history_DuringAZoom_DoesNotLoadAgain', async () => {
    const store = await setup();
    store.zoomTo(FROM, TO);
    await advance(1000);
    const callsBefore = getHistory.mock.calls.length;

    await advance(METRIC_CHART_RELOAD_INTERVAL_MS * 3);

    expect(getHistory.mock.calls.length).toBe(callsBefore);
  });

  it('history_AfterAReturnToAPreset_LoadsAgainEvery15Seconds', async () => {
    const store = await setup();
    store.selectCustomRange(FROM, TO);
    await advance(1000);
    store.selectRange(ChartRange.SixHours);
    await advance(1000);
    const callsBefore = getHistory.mock.calls.length;

    await advance(METRIC_CHART_RELOAD_INTERVAL_MS);

    expect(getHistory.mock.calls.length - callsBefore).toBe(1);
  });

  it('history_AfterTheStoreIsDestroyed_DoesNotLoadAgain', async () => {
    await setup();
    const callsBefore = getHistory.mock.calls.length;
    TestBed.resetTestingModule();

    await vi.advanceTimersByTimeAsync(METRIC_CHART_RELOAD_INTERVAL_MS * 3);

    expect(getHistory.mock.calls.length).toBe(callsBefore);
  });
});
