import { type CurrentMachineValues, DraftState, type MachineDraftDetail } from '@machines/drafts/draft';

import { CompareField, mergeValues, NO_CHOICES, toCompareRows, toCurrentFormValue } from './draft-compare.store.utils';

const CURRENT: CurrentMachineValues = {
  machineId: 'm1',
  name: 'Pump',
  serialNumber: 'SN-1',
  description: '<p>old</p>',
  rowVersion: 3,
};

const DRAFT: MachineDraftDetail = {
  id: 'd1',
  state: DraftState.Pending,
  isCreate: false,
  attemptId: 'a1',
  fileId: 'f1',
  name: 'Pump X',
  serialNumber: null,
  description: '<p>new</p>',
  rowVersion: 1,
};

describe('draft compare utils', () => {
  it('toCompareRows_WithNullProposal_CannotBeTaken', () => {
    const rows = toCompareRows(DRAFT, CURRENT, { taken: { serialNumber: true }, edited: {} });

    const serial = rows.find((row) => row.field === CompareField.SerialNumber);
    expect(serial).toMatchObject({ canTake: false, isTaken: false, proposed: null, current: 'SN-1' });
  });

  it('mergeValues_WithNothingTaken_KeepsTheCurrentValues', () => {
    const rows = toCompareRows(DRAFT, CURRENT, NO_CHOICES);

    expect(mergeValues(CURRENT, rows)).toEqual(toCurrentFormValue(CURRENT));
  });

  it('mergeValues_WithNameTaken_ChangesOnlyTheName', () => {
    const rows = toCompareRows(DRAFT, CURRENT, { taken: { name: true }, edited: {} });

    expect(mergeValues(CURRENT, rows)).toEqual({ name: 'Pump X', serialNumber: 'SN-1', description: '<p>old</p>' });
  });

  it('mergeValues_WithEditedTakenField_UsesTheEditedValue', () => {
    const rows = toCompareRows(DRAFT, CURRENT, {
      taken: { description: true },
      edited: { description: '<p>mine</p>' },
    });

    expect(mergeValues(CURRENT, rows).description).toBe('<p>mine</p>');
  });

  it('mergeValues_WithEditedButUntakenField_IgnoresTheEdit', () => {
    const rows = toCompareRows(DRAFT, CURRENT, { taken: {}, edited: { name: 'Edited' } });

    expect(mergeValues(CURRENT, rows).name).toBe('Pump');
  });
});
