import { WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';
import { FILE_MAX_COUNT } from '@work-orders/work-order-limits';
import {
  ESTIMATE_RANGE_ERROR_KEY,
  ESTIMATE_STEP_ERROR_KEY,
  TOO_MANY_FILES_ERROR_KEY,
  workOrderActionPayloadSchema,
  workOrderFormSchema,
  type WorkOrderFormValue,
} from './work-order-schema';

const VALID: WorkOrderFormValue = {
  title: 'Replace filter',
  description: null,
  priority: WorkOrderPriority.High,
  category: WorkOrderCategory.Repair,
  machineId: null,
  estimateMinutes: 60,
  fileIds: [],
};

function firstMessage(value: unknown): string | undefined {
  const result = workOrderFormSchema.safeParse(value);
  return result.success ? undefined : result.error.issues[0]?.message;
}

describe('workOrderFormSchema', () => {
  it.each([5, 720])('workOrderFormSchema_WithEstimateAtEdge_Passes (%i)', (estimateMinutes) => {
    const result = workOrderFormSchema.safeParse({ ...VALID, estimateMinutes });

    expect(result.success).toBe(true);
  });

  it.each([0, 4, 725])('workOrderFormSchema_WithEstimateOutsideRange_FailsWithRangeKey (%i)', (estimateMinutes) => {
    const message = firstMessage({ ...VALID, estimateMinutes });

    expect(message).toBe(ESTIMATE_RANGE_ERROR_KEY);
  });

  it('workOrderFormSchema_WithEstimateOffTheStep_FailsWithStepKey', () => {
    const message = firstMessage({ ...VALID, estimateMinutes: 62 });

    expect(message).toBe(ESTIMATE_STEP_ERROR_KEY);
  });

  it('workOrderFormSchema_WithTooManyFiles_FailsWithFilesKey', () => {
    const fileIds = Array.from({ length: FILE_MAX_COUNT + 1 }, (_, index) => `file-${index}`);

    const message = firstMessage({ ...VALID, fileIds });

    expect(message).toBe(TOO_MANY_FILES_ERROR_KEY);
  });

  it('workOrderActionPayloadSchema_WithOnlyATitle_Passes', () => {
    const result = workOrderActionPayloadSchema.safeParse({ title: 'Only a title' });

    expect(result.success).toBe(true);
  });
});
