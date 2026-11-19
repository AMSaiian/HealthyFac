import { DraftState, type WorkOrderDraft } from '@work-orders/drafts/work-order-draft';
import { toApplyBody, toFormValue } from '@work-orders/drafts/draft-apply-dialog/draft-apply-dialog.utils';
import { WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';

const DRAFT: WorkOrderDraft = {
  id: 'd1',
  machineId: 'm1',
  machineName: 'Lathe',
  isMachineDeleted: false,
  caseId: 'c1',
  runId: 'r1',
  title: 'Replace the bearing',
  description: null,
  priority: WorkOrderPriority.High,
  category: WorkOrderCategory.Repair,
  estimateMinutes: 60,
  state: DraftState.Pending,
  appliedWorkOrderId: null,
  proposedAt: '2026-01-01T00:00:00Z',
  rowVersion: 7,
};

describe('draft apply dialog', () => {
  it('toFormValue_DraftWithoutDescription_GivesAnEmptyTextNoFilesAndTheMachine', () => {
    expect(toFormValue(DRAFT)).toEqual({
      title: 'Replace the bearing',
      description: '',
      priority: WorkOrderPriority.High,
      category: WorkOrderCategory.Repair,
      machineId: 'm1',
      estimateMinutes: 60,
      fileIds: [],
    });
  });

  it('toApplyBody_EditedValues_SendsThemWithTheRowVersionOfTheDraft', () => {
    const value = {
      ...toFormValue(DRAFT),
      title: '  New title ',
      priority: WorkOrderPriority.Critical,
      estimateMinutes: 90,
    };

    expect(toApplyBody(DRAFT, value)).toEqual({
      rowVersion: 7,
      title: 'New title',
      description: null,
      priority: WorkOrderPriority.Critical,
      category: WorkOrderCategory.Repair,
      estimateMinutes: 90,
    });
  });

  it('toApplyBody_DescriptionWithText_KeepsTheTrimmedText', () => {
    const value = { ...toFormValue(DRAFT), description: ' Check the spindle ' };

    expect(toApplyBody(DRAFT, value).description).toBe('Check the spindle');
  });
});
