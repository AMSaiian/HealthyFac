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
  applicableStatuses,
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
  warningLow: null,
  warningHigh: null,
  alarmLow: null,
  alarmHigh: null,
  behavior: null,
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
  return {
    machineId: 'm1',
    machineName: 'Lathe',
    machineDraft: null,
    metrics: [],
    statuses: [],
    phases: [],
    ...overrides,
  };
}

describe('machineReason', { tags: ['Drafts'] }, () => {
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

describe('metricReason', { tags: ['Drafts'] }, () => {
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

describe('statusReason', { tags: ['Drafts'] }, () => {
  it('statusReason_WithoutCode_NeedsReview', () => {
    expect(statusReason({ ...STATUS, code: null })).toBe(QuickReason.StatusValues);
  });

  it('statusReason_WithCode_IsNull', () => {
    expect(statusReason(STATUS)).toBeNull();
  });
});

describe('needsReviewReasons', { tags: ['Drafts'] }, () => {
  it('needsReviewReasons_ForAwaitingTargetDrafts_GiveTheAwaitingReasonAndNeverApplyThem', () => {
    const drafts = {
      groups: [
        group({
          metrics: [{ ...METRIC, state: DraftState.AwaitingTarget }],
          statuses: [{ ...STATUS, state: DraftState.AwaitingTarget }],
        }),
      ],
    };

    const reasons = needsReviewReasons(drafts, CATALOG);

    expect(reasons).toEqual({ mt: QuickReason.AwaitingTarget, st: QuickReason.AwaitingTarget });
    expect(applicableMetrics(drafts.groups[0], CATALOG)).toEqual([]);
    expect(applicableStatuses(drafts.groups[0])).toEqual([]);
  });

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

describe('groupReason', { tags: ['Drafts'] }, () => {
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

describe('applicableMetrics', { tags: ['Drafts'] }, () => {
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

describe('machineSyncDelayMs', { tags: ['Drafts'] }, () => {
  it('machineSyncDelayMs_ForRetriesOneToFive_DoublesFromHalfASecond', () => {
    const delays = [1, 2, 3, 4, 5].map(machineSyncDelayMs);

    expect(delays).toEqual([500, 1000, 2000, 4000, 8000]);
  });
});

describe('retryAfterSeconds', { tags: ['Drafts'] }, () => {
  const tooMany = (headers: Record<string, string>): HttpErrorResponse =>
    new HttpErrorResponse({
      status: HttpStatusCode.TooManyRequests,
      headers: new HttpHeaders(headers),
    });

  it.each([
    ['AWholeNumberHeader', '2', 2],
    ['AFractionHeader', '1.2', 2],
  ])('retryAfterSeconds_With%s_ReturnsTheRoundedUpSeconds', (_name, header, expected) => {
    expect(retryAfterSeconds(tooMany({ 'Retry-After': header }))).toBe(expected);
  });

  it('retryAfterSeconds_WithoutAReadableHeader_ReturnsTheWholeWindow', () => {
    const answers = [tooMany({}), tooMany({ 'Retry-After': 'soon' }), tooMany({ 'Retry-After': '0' })];

    expect(answers.map(retryAfterSeconds)).toEqual([60, 60, 60]);
  });
});

describe('chunk', { tags: ['Drafts'] }, () => {
  it.each([
    ['WithMoreItemsThanTheSize_KeepsTheOrderAndTheLastPartShort', [1, 2, 3, 4, 5], 2, [[1, 2], [3, 4], [5]]],
    ['WithNoItems_ReturnsNoParts', [], 200, []],
  ])('chunk_%s', (_name, items, size, expected) => {
    expect(chunk(items, size)).toEqual(expected);
  });
});

describe('needsReviewReasons phase drafts', { tags: ['Drafts'] }, () => {
  const PHASE = {
    id: 'ph',
    state: DraftState.Pending,
    sourceLocation: null,
    code: 'idle',
    name: 'Idle',
    description: null,
    priority: 1,
    isTracked: true,
    conditions: [],
    limits: [],
  };

  const CONDITION = {
    metricDefinitionId: 'def',
    metricDraftId: null,
    statusDefinitionId: null,
    statusDraftId: null,
    operator: 'greaterThan',
    value: 1,
    upperValue: null,
    states: null,
  };
  const COMPLETE = { ...PHASE, conditions: [CONDITION] };
  const WAITING = { ...PHASE, conditions: [{ ...CONDITION, metricDefinitionId: null, metricDraftId: 'mt' }] };

  it.each([
    ['definitions only', COMPLETE, null],
    ['a pending metric draft', WAITING, QuickReason.PhaseWaitsForDrafts],
    ['no condition', PHASE, QuickReason.PhaseValues],
    ['no priority', { ...COMPLETE, priority: null }, QuickReason.PhaseValues],
  ])('needsReviewReasons_PhaseDraftWith_%s_GivesItsReason', (_name, phase, reason) => {
    const reasons = needsReviewReasons({ groups: [group({ phases: [phase] })] }, undefined);

    expect(reasons['ph']).toBe(reason);
  });

  it('groupReason_ForAGroupWithOnlyAnApplicablePhase_AllowsApplyAll', () => {
    const reason = groupReason(group({ metrics: [], statuses: [], phases: [COMPLETE] }), undefined);

    expect(reason).toBeNull();
  });

  it('groupReason_ForAGroupWithOnlyAWaitingPhase_HasNothingToApply', () => {
    const reason = groupReason(group({ metrics: [], statuses: [], phases: [WAITING] }), undefined);

    expect(reason).toBe(QuickReason.NothingToApply);
  });

  it('needsReviewReasons_PhaseDraftOfAPendingCreateMachine_WaitsForTheMachine', () => {
    const reasons = needsReviewReasons({ groups: [group({ machineId: null, phases: [PHASE] })] }, undefined);

    expect(reasons['ph']).toBe(QuickReason.MachinePending);
  });

  it('needsReviewReasons_PhaseDraftAwaitingItsTarget_ReturnsTheAwaitingReason', () => {
    const phase = { ...PHASE, state: DraftState.AwaitingTarget };

    const reasons = needsReviewReasons({ groups: [group({ phases: [phase] })] }, undefined);

    expect(reasons['ph']).toBe(QuickReason.AwaitingTarget);
  });

  it('needsReviewReasons_AppliedPhaseDraft_HasNoReason', () => {
    const phase = { ...PHASE, state: DraftState.Applied };

    const reasons = needsReviewReasons({ groups: [group({ phases: [phase] })] }, undefined);

    expect(reasons['ph']).toBeUndefined();
  });
});
