import { asList } from '@shared/grid/table/table-filters';

import {
  AgentRunState,
  CaseColumn,
  type CaseFilter,
  CaseSortColumn,
  CaseState,
  SignalSeverity,
} from '@maintenance/case';

export const COLUMN_LABEL_KEYS: Readonly<Record<CaseColumn, string>> = {
  [CaseColumn.Machine]: 'MAINTENANCE.COLUMNS.MACHINE',
  [CaseColumn.State]: 'MAINTENANCE.COLUMNS.STATE',
  [CaseColumn.Severity]: 'MAINTENANCE.COLUMNS.SEVERITY',
  [CaseColumn.Signals]: 'MAINTENANCE.COLUMNS.SIGNALS',
  [CaseColumn.LastRun]: 'MAINTENANCE.COLUMNS.LAST_RUN',
  [CaseColumn.OpenedAt]: 'MAINTENANCE.COLUMNS.OPENED_AT',
};

export const STATE_LABEL_KEYS: Readonly<Record<CaseState, string>> = {
  [CaseState.Open]: 'MAINTENANCE.STATES.OPEN',
  [CaseState.Resolved]: 'MAINTENANCE.STATES.RESOLVED',
  [CaseState.Closed]: 'MAINTENANCE.STATES.CLOSED',
};

export const SEVERITY_LABEL_KEYS: Readonly<Record<SignalSeverity, string>> = {
  [SignalSeverity.Warning]: 'MAINTENANCE.SEVERITIES.WARNING',
  [SignalSeverity.Alarm]: 'MAINTENANCE.SEVERITIES.ALARM',
};

export const RUN_STATE_LABEL_KEYS: Readonly<Record<AgentRunState, string>> = {
  [AgentRunState.Queued]: 'MAINTENANCE.RUN_STATES.QUEUED',
  [AgentRunState.Running]: 'MAINTENANCE.RUN_STATES.RUNNING',
  [AgentRunState.Completed]: 'MAINTENANCE.RUN_STATES.COMPLETED',
  [AgentRunState.Failed]: 'MAINTENANCE.RUN_STATES.FAILED',
  [AgentRunState.Interrupted]: 'MAINTENANCE.RUN_STATES.INTERRUPTED',
  [AgentRunState.Skipped]: 'MAINTENANCE.RUN_STATES.SKIPPED',
};

export const UNKNOWN_MACHINE_KEY = 'MAINTENANCE.UNKNOWN_MACHINE';
export const AI_UNAVAILABLE_KEY = 'MAINTENANCE.AI_UNAVAILABLE';
export const OPEN_TOOLTIP_KEY = 'MAINTENANCE.TOOLTIPS.OPEN';

export const SORTABLE_COLUMNS: readonly string[] = Object.values(CaseSortColumn);

/** Maps the pending value of one column filter of the table to the store filter. */
export const FILTER_READERS: Readonly<Record<CaseColumn, (value: unknown) => Partial<CaseFilter>>> = {
  [CaseColumn.Machine]: (value) => ({ machineIds: asList<string>(value) }),
  [CaseColumn.State]: (value) => ({ states: asList<CaseState>(value) }),
  [CaseColumn.Severity]: (value) => ({ severities: asList<SignalSeverity>(value) }),
  [CaseColumn.Signals]: () => ({}),
  [CaseColumn.LastRun]: (value) => toLastRunFilter(asList<string>(value)),
  [CaseColumn.OpenedAt]: () => ({}),
};

/** Reads the applied store value of one column as a table filter value. `null` means "no filter". */
export const FILTER_WRITERS: Readonly<Record<CaseColumn, (filter: CaseFilter) => unknown>> = {
  [CaseColumn.Machine]: (filter) => (filter.machineIds.length > 0 ? [...filter.machineIds] : null),
  [CaseColumn.State]: (filter) => (filter.states.length > 0 ? [...filter.states] : null),
  [CaseColumn.Severity]: (filter) => (filter.severities.length > 0 ? [...filter.severities] : null),
  [CaseColumn.Signals]: () => null,
  [CaseColumn.LastRun]: toLastRunValues,
  [CaseColumn.OpenedAt]: () => null,
};

/**
 * The value of the menu option "AI unavailable" in the filter menu of the last run column. The menu lists the run
 * states and this option.
 */
export const AI_UNAVAILABLE_OPTION = 'aiUnavailable';

const RUN_STATES: readonly string[] = Object.values(AgentRunState);

/** Splits the picked values of the last run menu into the run states and the "AI unavailable" flag. */
export function toLastRunFilter(values: readonly string[]): Pick<CaseFilter, 'lastRunStates' | 'isAiUnavailable'> {
  return {
    lastRunStates: values.filter((value): value is AgentRunState => RUN_STATES.includes(value)),
    isAiUnavailable: values.includes(AI_UNAVAILABLE_OPTION) ? true : null,
  };
}

/** The reverse of `toLastRunFilter`: the menu values of the applied filter, or `null` when no value is set. */
export function toLastRunValues(filter: CaseFilter): readonly string[] | null {
  const values = [...filter.lastRunStates, ...(filter.isAiUnavailable === true ? [AI_UNAVAILABLE_OPTION] : [])];
  return values.length > 0 ? values : null;
}

/** The columns that have a filter menu. */
export const FILTERABLE_COLUMNS: readonly CaseColumn[] = [
  CaseColumn.Machine,
  CaseColumn.State,
  CaseColumn.Severity,
  CaseColumn.LastRun,
];

/** The columns that show a text cell (`hf-cell-text`): their header has the minimum width of a text column. */
export const TEXT_COLUMNS: readonly CaseColumn[] = [CaseColumn.Machine];
