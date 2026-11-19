import {
  type AttemptDrafts,
  DraftState,
  type PhaseDraftConditionDetail,
  type PhaseDraftDetail,
  type PhaseDraftLimitDetail,
} from '@machines/drafts/draft';
import {
  findPhaseDraft,
  PhaseApplyBlock,
  phaseApplyBlock,
  phaseReviewBlock,
  toDraftFormValue,
  toPhaseBody,
  toSourceNames,
  waitsForDrafts,
} from '@machines/drafts/phase/phase-draft-review-store/phase-draft-review.store.utils';
import { PhaseConditionOperator } from '@machines/phases/operating-phase';
import { toBody } from '@machines/phases/phase-form/phase-form.utils';

const CONDITION: PhaseDraftConditionDetail = {
  metricDefinitionId: 'm1',
  metricDraftId: null,
  statusDefinitionId: null,
  statusDraftId: null,
  operator: PhaseConditionOperator.GreaterThan,
  value: 30,
  upperValue: null,
  states: null,
};

const LIMIT: PhaseDraftLimitDetail = {
  metricDefinitionId: 'm1',
  metricDraftId: null,
  warningLow: null,
  warningHigh: 75,
  alarmLow: null,
  alarmHigh: 85,
};

const DRAFT: PhaseDraftDetail = {
  id: 'p1',
  state: DraftState.Pending,
  sourceLocation: 'page 9',
  code: 'cutting',
  name: ' Cutting ',
  description: null,
  priority: 2,
  isTracked: true,
  conditions: [CONDITION],
  limits: [LIMIT],
};

const WAITING_CONDITION = { ...CONDITION, metricDefinitionId: null, metricDraftId: 'dm1' };
const WAITING_LIMIT = { ...LIMIT, metricDefinitionId: null, metricDraftId: 'dm1' };

describe('phaseApplyBlock', () => {
  it('phaseApplyBlock_CompleteDraftOfDefinitions_ReturnsNull', () => {
    expect(phaseApplyBlock(DRAFT)).toBeNull();
  });

  it.each<[string, Partial<PhaseDraftDetail>, PhaseApplyBlock]>([
    ['a condition on a metric draft', { conditions: [WAITING_CONDITION] }, PhaseApplyBlock.WaitsForDrafts],
    ['a limit row on a metric draft', { limits: [WAITING_LIMIT] }, PhaseApplyBlock.WaitsForDrafts],
    [
      'a status draft in a condition',
      { conditions: [{ ...CONDITION, metricDefinitionId: null, statusDraftId: 'ds1' }] },
      PhaseApplyBlock.WaitsForDrafts,
    ],
    ['no code', { code: null }, PhaseApplyBlock.NeedsValues],
    ['a blank name', { name: '  ' }, PhaseApplyBlock.NeedsValues],
    ['no priority', { priority: null }, PhaseApplyBlock.NeedsValues],
    ['no tracking flag', { isTracked: null }, PhaseApplyBlock.NeedsValues],
    ['no condition', { conditions: [] }, PhaseApplyBlock.NeedsValues],
    ['no operator', { conditions: [{ ...CONDITION, operator: null }] }, PhaseApplyBlock.NeedsValues],
    ['an unknown operator', { conditions: [{ ...CONDITION, operator: 'near' }] }, PhaseApplyBlock.NeedsValues],
    [
      'a condition without a source',
      { conditions: [{ ...CONDITION, metricDefinitionId: null }] },
      PhaseApplyBlock.NeedsValues,
    ],
    ['a limit row without a metric', { limits: [{ ...LIMIT, metricDefinitionId: null }] }, PhaseApplyBlock.NeedsValues],
  ])('phaseApplyBlock_With%s_ReturnsItsBlock', (_name, change, expected) => {
    expect(phaseApplyBlock({ ...DRAFT, ...change })).toBe(expected);
  });

  it('phaseApplyBlock_WithWaitingDraftAndMissingCode_ReportsTheWaitFirst', () => {
    expect(phaseApplyBlock({ ...DRAFT, code: null, limits: [WAITING_LIMIT] })).toBe(PhaseApplyBlock.WaitsForDrafts);
  });
});

describe('waitsForDrafts', () => {
  it('waitsForDrafts_DraftOfDefinitions_IsFalse', () => {
    expect(waitsForDrafts(DRAFT)).toBe(false);
  });

  it('waitsForDrafts_LimitRowOnDraft_IsTrue', () => {
    expect(waitsForDrafts({ ...DRAFT, limits: [WAITING_LIMIT] })).toBe(true);
  });
});

describe('toPhaseBody', () => {
  it('toPhaseBody_CompleteDraft_BuildsTheBodyWithTrimmedValues', () => {
    expect(toPhaseBody(DRAFT)).toEqual({
      code: 'cutting',
      name: 'Cutting',
      description: null,
      priority: 2,
      isTracked: true,
      conditions: [
        {
          metricDefinitionId: 'm1',
          statusDefinitionId: null,
          operator: PhaseConditionOperator.GreaterThan,
          value: 30,
          upperValue: null,
          states: null,
        },
      ],
      limits: [{ metricDefinitionId: 'm1', warningLow: null, warningHigh: 75, alarmLow: null, alarmHigh: 85 }],
    });
  });

  it('toPhaseBody_DraftThatWaitsForADraft_ReturnsNull', () => {
    expect(toPhaseBody({ ...DRAFT, conditions: [WAITING_CONDITION] })).toBeNull();
  });
});

describe('findPhaseDraft', () => {
  const DRAFTS: AttemptDrafts = {
    groups: [
      { machineId: 'a', machineName: null, machineDraft: null, metrics: [], statuses: [], phases: [] },
      { machineId: 'b', machineName: null, machineDraft: null, metrics: [], statuses: [], phases: [DRAFT] },
    ],
  };

  it('findPhaseDraft_DraftInASecondGroup_FindsIt', () => {
    expect(findPhaseDraft(DRAFTS, 'p1')).toBe(DRAFT);
  });

  it('findPhaseDraft_UnknownId_ReturnsNull', () => {
    expect(findPhaseDraft(DRAFTS, 'other')).toBeNull();
  });
});

describe('toSourceNames', () => {
  it('toSourceNames_DefinitionsAndDrafts_NamesBothByTheirIds', () => {
    const drafts: AttemptDrafts = {
      groups: [
        {
          machineId: 'a',
          machineName: null,
          machineDraft: null,
          metrics: [{ id: 'dm1', name: null, key: 'coolant.level' } as never],
          statuses: [{ id: 'ds1', code: 'E104' } as never],
          phases: [],
        },
      ],
    };

    const names = toSourceNames(
      drafts,
      [{ id: 'm1', name: 'Spindle temperature' } as never],
      [{ id: 's1', code: 'S002' } as never],
    );

    expect(Object.fromEntries(names.metrics)).toEqual({ m1: 'Spindle temperature', dm1: 'coolant.level' });
    expect(Object.fromEntries(names.statuses)).toEqual({ s1: 'S002', ds1: 'E104' });
  });
});

describe('editable phase draft review', () => {
  const NO_PRIORITY: PhaseDraftDetail = { ...DRAFT, priority: null, isTracked: null, conditions: [CONDITION] };

  it('phaseReviewBlock_ForADraftWithoutAPriority_NeedsValuesUntilAPriorityIsSet', () => {
    const withoutPriority = phaseReviewBlock(NO_PRIORITY, null, true);
    const withPriority = phaseReviewBlock(NO_PRIORITY, 2, true);

    expect(withoutPriority).toBe(PhaseApplyBlock.NeedsValues);
    expect(withPriority).toBeNull();
  });

  it('phaseReviewBlock_WithAnInvalidForm_NeedsValues', () => {
    expect(phaseReviewBlock(NO_PRIORITY, 2, false)).toBe(PhaseApplyBlock.NeedsValues);
  });

  it('phaseReviewBlock_ForADraftThatNamesAPendingDraft_WaitsForDraftsBeforeAnyOtherReason', () => {
    const waiting = { ...NO_PRIORITY, conditions: [{ ...CONDITION, metricDefinitionId: null, metricDraftId: 'mt' }] };

    expect(phaseReviewBlock(waiting, null, false)).toBe(PhaseApplyBlock.WaitsForDrafts);
  });

  it('toDraftFormValue_ForADraftWithoutTrackingFlagAndValues_FillsTheFormDefaults', () => {
    const value = toDraftFormValue({ ...NO_PRIORITY, code: null, name: null });

    expect(value).toMatchObject({ code: '', name: '', isTracked: true });
    expect(value.conditions[0]).toMatchObject({ sourceId: 'm1', operator: PhaseConditionOperator.GreaterThan });
  });

  it('toBody_WithTheEditedValueOfADraftAndThePriority_SendsTheEditedValues', () => {
    const edited = { ...toDraftFormValue(NO_PRIORITY), code: 'auto', name: ' Auto ' };

    const body = toBody(edited, 2, new Set());

    expect(body).toMatchObject({ code: 'auto', name: 'Auto', priority: 2, isTracked: true });
    expect(body.conditions).toHaveLength(1);
  });
});
