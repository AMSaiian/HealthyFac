import { type AttemptDraftGroup, DraftKind, DraftState, type MachineDraftDetail } from '@machines/drafts/draft';

import { toDraftGroups } from './ingest-summary.store.utils';

const CREATE: MachineDraftDetail = {
  id: 'md1',
  state: DraftState.Pending,
  isCreate: true,
  attemptId: 'a1',
  fileId: 'f1',
  name: 'Pump',
  serialNumber: null,
  description: null,
  rowVersion: 1,
};

const METRIC = {
  id: 'm1',
  state: DraftState.Pending,
  sourceLocation: 'page 1',
  key: 'inlet.pressure',
  name: null,
  description: null,
  valueKind: 'numeric',
  quantityKindName: null,
  unitName: null,
  min: null,
  max: 6,
  states: null,
};

const STATUS = {
  id: 's1',
  state: DraftState.Applied,
  sourceLocation: 'page 2',
  code: 'E12',
  severity: 'error',
  description: null,
};

describe('toDraftGroups', () => {
  it('toDraftGroups_WithPendingCreateDraft_DisablesTheReviewOfItsChildren', () => {
    const group: AttemptDraftGroup = {
      machineId: null,
      machineName: null,
      machineDraft: CREATE,
      metrics: [METRIC],
      statuses: [STATUS],
    };

    const [view] = toDraftGroups([group]);

    expect(view.titleKey).toBe('DRAFTS.GROUP.NEW_MACHINE');
    expect(view.titleName).toBe('Pump');
    expect(view.rows.map((row) => [row.kind, row.isReviewDisabled])).toEqual([
      [DraftKind.Machine, false],
      [DraftKind.Metric, true],
      [DraftKind.Status, true],
    ]);
    expect(view.rows[2].isPending).toBe(false);
  });

  it('toDraftGroups_WithChildrenOfAKnownMachine_KeepsTheirReviewEnabledUnderTheMachineName', () => {
    const group: AttemptDraftGroup = {
      machineId: 'machine-1',
      machineName: 'Press',
      machineDraft: null,
      metrics: [METRIC],
      statuses: [],
    };

    const [view] = toDraftGroups([group]);

    expect(view.titleKey).toBe('DRAFTS.GROUP.MACHINE');
    expect(view.titleName).toBe('Press');
    expect(view.rows).toHaveLength(1);
    expect(view.rows[0].isReviewDisabled).toBe(false);
    expect(view.rows[0].label).toBe('inlet.pressure');
  });

  it('toDraftGroups_WithUnnamedCreateDraft_UsesTheUnnamedTitle', () => {
    const group: AttemptDraftGroup = {
      machineId: null,
      machineName: null,
      machineDraft: { ...CREATE, name: null },
      metrics: [],
      statuses: [],
    };

    expect(toDraftGroups([group])[0].titleKey).toBe('DRAFTS.GROUP.NEW_MACHINE_UNNAMED');
  });

  it('toDraftGroups_WithAppliedCreateDraft_EnablesTheReviewOfItsChildren', () => {
    const group: AttemptDraftGroup = {
      machineId: 'machine-1',
      machineName: 'Pump',
      machineDraft: { ...CREATE, state: DraftState.Applied },
      metrics: [METRIC],
      statuses: [],
    };

    expect(toDraftGroups([group])[0].rows[1].isReviewDisabled).toBe(false);
  });
});
