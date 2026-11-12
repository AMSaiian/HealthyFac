import { DraftKind, DraftState, type MachineDraftGridItem } from '@machines/drafts/draft';

import { isReviewDisabledOf, machineTitleOf } from './draft-grid.utils';

const ROW: MachineDraftGridItem = {
  id: 'd1',
  kind: DraftKind.Metric,
  state: DraftState.Pending,
  attemptId: 'a1',
  fileId: 'f1',
  proposedAt: '2026-01-01T00:00:00Z',
  sourceLocation: null,
  label: 'Pressure',
  rowVersion: 1,
  isCreate: false,
  machineId: 'm1',
  machineName: 'Press',
};

describe('machineTitleOf', () => {
  it('machineTitleOf_WithKnownMachine_UsesTheMachineTitleAndName', () => {
    const title = machineTitleOf(ROW);

    expect(title).toEqual({ key: 'DRAFTS.GROUP.MACHINE', name: 'Press' });
  });

  it('machineTitleOf_WithPendingCreateDraft_UsesTheProposedNameOrTheUnnamedTitle', () => {
    const named = machineTitleOf({ ...ROW, machineId: null, machineName: 'Pump' });
    const unnamed = machineTitleOf({ ...ROW, machineId: null, machineName: null });

    expect(named).toEqual({ key: 'DRAFTS.GROUP.NEW_MACHINE', name: 'Pump' });
    expect(unnamed.key).toBe('DRAFTS.GROUP.NEW_MACHINE_UNNAMED');
  });
});

describe('isReviewDisabledOf', () => {
  it('isReviewDisabledOf_ForChildOfPendingCreateDraft_ReturnsTrueButNotForTheMachineDraft', () => {
    const child = isReviewDisabledOf({ ...ROW, machineId: null });
    const machine = isReviewDisabledOf({ ...ROW, kind: DraftKind.Machine, machineId: null, isCreate: true });

    expect(child).toBe(true);
    expect(machine).toBe(false);
  });

  it('isReviewDisabledOf_ForChildOfKnownMachine_ReturnsFalse', () => {
    const child = isReviewDisabledOf(ROW);

    expect(child).toBe(false);
  });
});
