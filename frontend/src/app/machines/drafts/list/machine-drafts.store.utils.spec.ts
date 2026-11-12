import { DraftKind, DraftState, type MachineDraftGridItem } from '@machines/drafts/draft';

import { toApplyList, withSelected } from './machine-drafts.store.utils';

function item(id: string, kind: DraftKind): MachineDraftGridItem {
  return {
    id,
    kind,
    state: DraftState.Pending,
    attemptId: 'a1',
    fileId: 'f1',
    proposedAt: '2026-01-01T00:00:00Z',
    sourceLocation: null,
    label: id,
    rowVersion: 1,
    isCreate: false,
    machineId: 'm1',
    machineName: 'Press',
  };
}

describe('withSelected', () => {
  it('withSelected_CheckedThenUnchecked_KeepsOtherChecksAndLeavesTheInputUntouched', () => {
    const first = item('d1', DraftKind.Metric);
    const second = item('d2', DraftKind.Status);
    const start = withSelected({}, [first, second], true);

    const next = withSelected(start, [first], false);

    expect(Object.keys(next)).toEqual(['d2']);
    expect(Object.keys(start)).toEqual(['d1', 'd2']);
  });
});

describe('toApplyList', () => {
  it('toApplyList_WithMixedKinds_PutsMachineDraftsFirstThenMetricsThenStatuses', () => {
    const items = [item('s1', DraftKind.Status), item('m1', DraftKind.Metric), item('md', DraftKind.Machine)];

    const list = toApplyList(items);

    expect(list.map((ref) => ref.draftId)).toEqual(['md', 'm1', 's1']);
    expect(list[0].attemptId).toBe('a1');
  });
});
