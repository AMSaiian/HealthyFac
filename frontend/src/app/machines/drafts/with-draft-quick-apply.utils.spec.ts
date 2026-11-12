import { HttpErrorResponse, HttpHeaders, HttpStatusCode } from '@angular/common/http';

import {
  type AttemptDraftGroup,
  DraftState,
  type MachineDraftDetail,
  type MetricDraftDetail,
  type StatusDraftDetail,
} from '@machines/drafts/draft';
import type { MetricUnitsResponse } from '@machines/metrics/metric-definition';

import {
  applicableMetrics,
  chunk,
  groupReason,
  machineReason,
  machineSyncDelayMs,
  metricReason,
  needsReviewReasons,
  QuickReason,
  retryAfterSeconds,
  statusReason,
} from './with-draft-quick-apply.utils';

const CATALOG: MetricUnitsResponse = {
  kinds: [{ name: 'Temperature', units: [{ name: 'Celsius', abbreviation: '°C' }] }],
} as unknown as MetricUnitsResponse;

const MACHINE: MachineDraftDetail = {
  id: 'md',
  state: DraftState.Pending,
  isCreate: true,
  attemptId: 'a1',
  fileId: 'f1',
  name: 'Lathe',
  serialNumber: 'S-1',
  description: null,
  rowVersion: 1,
};

const METRIC: MetricDraftDetail = {
  id: 'mt',
  state: DraftState.Pending,
  sourceLocation: null,
  key: 'spindle.temperature',
  name: 'Spindle temperature',
  description: null,
  valueKind: 'numeric',
  quantityKindName: 'Temperature',
  unitName: 'Celsius',
  min: null,
  max: null,
  states: null,
};

const STATUS: StatusDraftDetail = {
  id: 'st',
  state: DraftState.Pending,
  sourceLocation: null,
  code: 'E12',
  severity: 'warning',
  description: null,
};

function group(overrides: Partial<AttemptDraftGroup> = {}): AttemptDraftGroup {
  return { machineId: 'm1', machineName: 'Lathe', machineDraft: null, metrics: [], statuses: [], ...overrides };
}

describe('machineReason', () => {
  it.each([
    [{ name: null }, QuickReason.MachineValues],
    [{ name: '  ' }, QuickReason.MachineValues],
    [{ serialNumber: null }, QuickReason.MachineValues],
    [{}, null],
    [{ isCreate: false, name: null, serialNumber: null }, null],
  ])('machineReason_ForCreateDraftWith%j_Returns%s', (overrides, expected) => {
    expect(machineReason({ ...MACHINE, ...overrides })).toBe(expected);
  });
});

describe('metricReason', () => {
  it.each([
    [{}, CATALOG, null],
    [{ name: null }, CATALOG, QuickReason.MetricValues],
    [{ key: null }, CATALOG, QuickReason.MetricValues],
    [{ unitName: 'Parsec' }, CATALOG, QuickReason.UnknownUnit],
    [{ quantityKindName: null, unitName: null }, CATALOG, QuickReason.UnknownUnit],
    [{}, undefined, QuickReason.UnitsLoading],
    [{ valueKind: 'stateSet', states: null }, CATALOG, QuickReason.MissingStates],
    [{ valueKind: 'stateSet', states: [] }, CATALOG, QuickReason.MissingStates],
    [{ valueKind: 'stateSet', states: ['on'], quantityKindName: null, unitName: null }, CATALOG, null],
    [{ valueKind: 'boolean', quantityKindName: null, unitName: null }, CATALOG, null],
  ])('metricReason_ForDraftWith%j_Returns%s', (overrides, catalog, expected) => {
    expect(metricReason({ ...METRIC, ...overrides }, catalog)).toBe(expected);
  });
});

describe('statusReason', () => {
  it('statusReason_WithoutCode_NeedsReview', () => {
    expect(statusReason({ ...STATUS, code: null })).toBe(QuickReason.StatusValues);
  });

  it('statusReason_WithCode_IsNull', () => {
    expect(statusReason(STATUS)).toBeNull();
  });
});

describe('needsReviewReasons', () => {
  it('needsReviewReasons_ForChildrenOfAPendingCreateDraft_WaitForTheMachine', () => {
    const drafts = {
      groups: [group({ machineId: null, machineDraft: MACHINE, metrics: [METRIC], statuses: [STATUS] })],
    };

    const reasons = needsReviewReasons(drafts, CATALOG);

    expect(reasons).toEqual({
      md: null,
      mt: QuickReason.MachinePending,
      st: QuickReason.MachinePending,
    });
  });

  it('needsReviewReasons_ForAnAppliedMachineDraft_ChecksTheChildrenAndSkipsNonPendingDrafts', () => {
    const drafts = {
      groups: [
        group({
          machineDraft: { ...MACHINE, state: DraftState.Applied },
          metrics: [
            { ...METRIC, unitName: 'Parsec' },
            { ...METRIC, id: 'done', state: DraftState.Applied },
          ],
          statuses: [STATUS],
        }),
      ],
    };

    const reasons = needsReviewReasons(drafts, CATALOG);

    expect(reasons).toEqual({ mt: QuickReason.UnknownUnit, st: null });
  });
});

describe('groupReason', () => {
  it('groupReason_WithPendingMachineWithoutName_ReturnsTheMachineReason', () => {
    const value = group({ machineId: null, machineDraft: { ...MACHINE, name: null }, metrics: [METRIC] });

    expect(groupReason(value, CATALOG)).toBe(QuickReason.MachineValues);
  });

  it('groupReason_WithPendingCreateDraftAndChildren_IsNullBecauseTheMachineIsAppliedFirst', () => {
    const value = group({ machineId: null, machineDraft: MACHINE, metrics: [METRIC] });

    expect(groupReason(value, CATALOG)).toBeNull();
  });

  it('groupReason_WhenEveryChildNeedsReview_ReturnsNothingToApply', () => {
    const value = group({ metrics: [{ ...METRIC, unitName: 'Parsec' }], statuses: [{ ...STATUS, code: null }] });

    expect(groupReason(value, CATALOG)).toBe(QuickReason.NothingToApply);
  });

  it('groupReason_WithOneApplicableChild_IsNull', () => {
    const value = group({ metrics: [{ ...METRIC, unitName: 'Parsec' }], statuses: [STATUS] });

    expect(groupReason(value, CATALOG)).toBeNull();
  });
});

describe('applicableMetrics', () => {
  it('applicableMetrics_WithMixedDrafts_KeepsPendingDraftsWithoutAReason', () => {
    const value = group({
      metrics: [
        METRIC,
        { ...METRIC, id: 'bad', unitName: 'Parsec' },
        { ...METRIC, id: 'old', state: DraftState.Applied },
      ],
    });

    expect(applicableMetrics(value, CATALOG).map((draft) => draft.id)).toEqual(['mt']);
  });
});

describe('machineSyncDelayMs', () => {
  it('machineSyncDelayMs_ForRetriesOneToFive_DoublesFromHalfASecond', () => {
    const delays = [1, 2, 3, 4, 5].map(machineSyncDelayMs);

    expect(delays).toEqual([500, 1000, 2000, 4000, 8000]);
  });
});

describe('retryAfterSeconds', () => {
  const tooMany = (headers: Record<string, string>): HttpErrorResponse =>
    new HttpErrorResponse({
      status: HttpStatusCode.TooManyRequests,
      headers: new HttpHeaders(headers),
    });

  it('retryAfterSeconds_WithAWholeNumberHeader_ReturnsIt', () => {
    expect(retryAfterSeconds(tooMany({ 'Retry-After': '2' }))).toBe(2);
  });

  it('retryAfterSeconds_WithAFractionHeader_RoundsUp', () => {
    expect(retryAfterSeconds(tooMany({ 'Retry-After': '1.2' }))).toBe(2);
  });

  it('retryAfterSeconds_WithoutAReadableHeader_ReturnsTheWholeWindow', () => {
    const answers = [tooMany({}), tooMany({ 'Retry-After': 'soon' }), tooMany({ 'Retry-After': '0' })];

    expect(answers.map(retryAfterSeconds)).toEqual([60, 60, 60]);
  });
});

describe('chunk', () => {
  it('chunk_WithMoreItemsThanTheSize_KeepsTheOrderAndTheLastPartShort', () => {
    expect(chunk([1, 2, 3, 4, 5], 2)).toEqual([[1, 2], [3, 4], [5]]);
  });

  it('chunk_WithNoItems_ReturnsNoParts', () => {
    expect(chunk([], 200)).toEqual([]);
  });
});
