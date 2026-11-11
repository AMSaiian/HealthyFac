import {
  array,
  extend,
  type infer as Infer,
  int,
  maxLength,
  minLength,
  nullable,
  number,
  object,
  optional,
  partial,
  refine,
  string,
  trim,
  enum as zodEnum,
  type ZodMiniString,
} from 'zod/mini';

import { richTextSchema } from '@shared/rich-text/rich-text-schema';

import { WorkOrderCategory, WorkOrderPriority } from '@work-orders/work-order';
import {
  DESCRIPTION_MAX_LENGTH,
  ESTIMATE_MAX_MINUTES,
  ESTIMATE_MIN_MINUTES,
  ESTIMATE_STEP_MINUTES,
  FILE_MAX_COUNT,
  TITLE_MAX_LENGTH,
} from '@work-orders/work-order-limits';

export const REQUIRED_ERROR_KEY = 'WORK_ORDERS.FORM.ERRORS.REQUIRED';
export const TOO_LONG_ERROR_KEY = 'WORK_ORDERS.FORM.ERRORS.TOO_LONG';
export const ESTIMATE_RANGE_ERROR_KEY = 'WORK_ORDERS.FORM.ERRORS.ESTIMATE_RANGE';
export const ESTIMATE_STEP_ERROR_KEY = 'WORK_ORDERS.FORM.ERRORS.ESTIMATE_STEP';
export const TOO_MANY_FILES_ERROR_KEY = 'WORK_ORDERS.FORM.ERRORS.TOO_MANY_FILES';

const requiredText = (limit: number): ZodMiniString =>
  string().check(trim(), minLength(1, REQUIRED_ERROR_KEY), maxLength(limit, TOO_LONG_ERROR_KEY));

/**
 * The one schema of a work order. The drawer form validates with it, and the action dispatcher validates the payload
 * of an action with its partial form. The messages are translation keys. The limits mirror the backend.
 */
export const workOrderFormSchema = object({
  title: requiredText(TITLE_MAX_LENGTH),
  description: richTextSchema(DESCRIPTION_MAX_LENGTH, TOO_LONG_ERROR_KEY),
  priority: zodEnum(WorkOrderPriority),
  category: zodEnum(WorkOrderCategory),
  machineId: nullable(string()),
  estimateMinutes: number().check(
    int(ESTIMATE_RANGE_ERROR_KEY),
    refine((minutes) => minutes >= ESTIMATE_MIN_MINUTES && minutes <= ESTIMATE_MAX_MINUTES, ESTIMATE_RANGE_ERROR_KEY),
    refine((minutes) => minutes % ESTIMATE_STEP_MINUTES === 0, ESTIMATE_STEP_ERROR_KEY),
  ),
  fileIds: array(string()).check(maxLength(FILE_MAX_COUNT, TOO_MANY_FILES_ERROR_KEY)),
});

export type WorkOrderFormValue = Infer<typeof workOrderFormSchema>;

export const workOrderActionPayloadSchema = extend(partial(workOrderFormSchema), {
  workOrderId: optional(string()),
});

export type WorkOrderActionPayload = Infer<typeof workOrderActionPayloadSchema>;
