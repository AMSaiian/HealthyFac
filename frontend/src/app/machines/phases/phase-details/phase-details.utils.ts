import { EMPTY_CELL } from '@shared/grid/table/grid-display';

import type { MetricDefinition } from '@machines/metrics/metric-definition';
import type { ConditionSourceOption } from '@machines/phases/phase-form/phase-form.utils';

/** The fraction digits of a number input in the phase form (`p-inputnumber`). The view shows the same digits. */
const NUMBER_FRACTION_DIGITS = 6;
const STATES_SEPARATOR = ', ';

/** The label of the condition source (a status or a metric), or the empty cell when the options do not hold it. */
export function sourceLabel(sourceId: string, options: readonly ConditionSourceOption[]): string {
  return options.find((option) => option.id === sourceId)?.label ?? EMPTY_CELL;
}

/** The name of the metric, or the empty cell when the list does not hold it. */
export function metricName(metricId: string, metrics: readonly MetricDefinition[]): string {
  return metrics.find((metric) => metric.id === metricId)?.name ?? EMPTY_CELL;
}

/**
 * The text of a number in the format of the number input of the form: the language of the browser, at most 6
 * fraction digits.
 */
export function numberText(value: number | null): string {
  if (value === null) {
    return EMPTY_CELL;
  }
  return new Intl.NumberFormat(undefined, { maximumFractionDigits: NUMBER_FRACTION_DIGITS }).format(value);
}

/** The states of a condition, joined with commas, or the empty cell when there is none. */
export function statesText(states: readonly string[] | null): string {
  return states === null || states.length === 0 ? EMPTY_CELL : states.join(STATES_SEPARATOR);
}
