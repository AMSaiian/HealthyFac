import { WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';
import {
  ESTIMATE_STEP_ERROR_KEY,
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
  assigneeId: null,
  start: null,
};

function firstMessage(value: unknown): string | undefined {
  const result = workOrderFormSchema.safeParse(value);
  return result.success ? undefined : result.error.issues[0]?.message;
}

describe('workOrderFormSchema', { tags: ['WorkOrders'] }, () => {
  it('workOrderFormSchema_WithEstimateOffTheStep_FailsWithStepKey', () => {
    const message = firstMessage({ ...VALID, estimateMinutes: 62 });

    expect(message).toBe(ESTIMATE_STEP_ERROR_KEY);
  });

  it('workOrderActionPayloadSchema_WithOnlyATitle_Passes', () => {
    const result = workOrderActionPayloadSchema.safeParse({ title: 'Only a title' });

    expect(result.success).toBe(true);
  });
});
