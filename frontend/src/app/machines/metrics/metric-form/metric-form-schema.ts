import {
  array,
  type infer as Infer,
  maxLength,
  minLength,
  nullable,
  number,
  object,
  refine,
  regex,
  string,
  trim,
  enum as zodEnum,
  type ZodMiniString,
} from 'zod/mini';

import { richTextSchema } from '@shared/rich-text/rich-text-schema';

import { MetricValueKind } from '@machines/metrics/metric-definition';
import {
  DESCRIPTION_MAX_LENGTH,
  KEY_MAX_LENGTH,
  KEY_PATTERN,
  MIN_STATE_COUNT,
  NAME_MAX_LENGTH,
} from '@machines/metrics/metric-limits';

export const REQUIRED_ERROR_KEY = 'FORMS.REQUIRED';
export const TOO_LONG_ERROR_KEY = 'FORMS.TOO_LONG';
export const KEY_INVALID_ERROR_KEY = 'METRICS.FORM.ERRORS.KEY_INVALID';
export const KEY_TAKEN_ERROR_KEY = 'METRICS.FORM.ERRORS.KEY_TAKEN';
export const UNIT_REQUIRED_ERROR_KEY = 'METRICS.FORM.ERRORS.UNIT_REQUIRED';
export const RANGE_ERROR_KEY = 'METRICS.FORM.ERRORS.RANGE';
export const STATES_TOO_FEW_ERROR_KEY = 'METRICS.FORM.ERRORS.STATES_TOO_FEW';
export const STATES_DUPLICATED_ERROR_KEY = 'METRICS.FORM.ERRORS.STATES_DUPLICATED';

const requiredText = (limit: number): ZodMiniString =>
  string().check(trim(), minLength(1, REQUIRED_ERROR_KEY), maxLength(limit, TOO_LONG_ERROR_KEY));

const keyText = (): ZodMiniString =>
  string().check(
    minLength(1, REQUIRED_ERROR_KEY),
    maxLength(KEY_MAX_LENGTH, TOO_LONG_ERROR_KEY),
    regex(KEY_PATTERN, KEY_INVALID_ERROR_KEY),
  );

/**
 * The one schema of a metric. The form validates with it, and the changes compare through it. The unit is the encoded
 * reference of the unit catalog (`unitValue`), or an empty string for no unit. The messages are translation keys.
 */
export const metricFormSchema = object({
  key: keyText(),
  name: requiredText(NAME_MAX_LENGTH),
  description: richTextSchema(DESCRIPTION_MAX_LENGTH, TOO_LONG_ERROR_KEY),
  valueKind: zodEnum(MetricValueKind),
  unit: string(),
  min: nullable(number()),
  max: nullable(number()),
  states: array(keyText()),
}).check(
  refine((value) => value.valueKind !== MetricValueKind.Numeric || value.unit !== '', {
    path: ['unit'],
    error: UNIT_REQUIRED_ERROR_KEY,
  }),
  refine(
    (value) =>
      value.valueKind !== MetricValueKind.Numeric || value.min === null || value.max === null || value.min < value.max,
    { path: ['max'], error: RANGE_ERROR_KEY },
  ),
  refine((value) => value.valueKind !== MetricValueKind.StateSet || value.states.length >= MIN_STATE_COUNT, {
    path: ['states'],
    error: STATES_TOO_FEW_ERROR_KEY,
  }),
  refine(
    (value) => value.valueKind !== MetricValueKind.StateSet || new Set(value.states).size === value.states.length,
    {
      path: ['states'],
      error: STATES_DUPLICATED_ERROR_KEY,
    },
  ),
);

export type MetricFormValue = Infer<typeof metricFormSchema>;
