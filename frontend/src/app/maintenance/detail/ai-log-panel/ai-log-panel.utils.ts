import {
  AgentRole,
  AgentRunErrorCode,
  AgentRunSkipReason,
  AgentRunVerdict,
  type CaseToolCall,
  SignalRule,
  ToolCallOutcome,
} from '@maintenance/case';
import { SEVERITY_LABEL_KEYS } from '@maintenance/list/cases-grid/cases-grid.utils';

export const VERDICT_LABEL_KEYS: Readonly<Record<AgentRunVerdict, string>> = {
  [AgentRunVerdict.Dismiss]: 'MAINTENANCE.VERDICTS.DISMISS',
  [AgentRunVerdict.Watch]: 'MAINTENANCE.VERDICTS.WATCH',
  [AgentRunVerdict.Escalate]: 'MAINTENANCE.VERDICTS.ESCALATE',
  [AgentRunVerdict.Safe]: 'MAINTENANCE.VERDICTS.SAFE',
  [AgentRunVerdict.Unsafe]: 'MAINTENANCE.VERDICTS.UNSAFE',
};

export const RUN_ERROR_LABEL_KEYS: Readonly<Record<AgentRunErrorCode, string>> = {
  [AgentRunErrorCode.ProviderUnavailable]: 'MAINTENANCE.RUN_ERRORS.PROVIDER_UNAVAILABLE',
  [AgentRunErrorCode.UnexpectedError]: 'MAINTENANCE.RUN_ERRORS.UNEXPECTED_ERROR',
  [AgentRunErrorCode.CaseChanged]: 'MAINTENANCE.RUN_ERRORS.CASE_CHANGED',
  [AgentRunErrorCode.NoDecision]: 'MAINTENANCE.RUN_ERRORS.NO_DECISION',
};

export const SKIP_REASON_LABEL_KEYS: Readonly<Record<AgentRunSkipReason, string>> = {
  [AgentRunSkipReason.CaseNotOpen]: 'MAINTENANCE.RUN_SKIP_REASONS.CASE_NOT_OPEN',
  [AgentRunSkipReason.NothingToReview]: 'MAINTENANCE.RUN_SKIP_REASONS.NOTHING_TO_REVIEW',
};

export const OUTCOME_LABEL_KEYS: Readonly<Record<ToolCallOutcome, string>> = {
  [ToolCallOutcome.Succeeded]: 'MAINTENANCE.OUTCOMES.SUCCEEDED',
  [ToolCallOutcome.Rejected]: 'MAINTENANCE.OUTCOMES.REJECTED',
  [ToolCallOutcome.Failed]: 'MAINTENANCE.OUTCOMES.FAILED',
};

export const ROLE_LABEL_KEYS: Readonly<Record<AgentRole, string>> = {
  [AgentRole.Triage]: 'MAINTENANCE.ROLES.TRIAGE',
  [AgentRole.Planner]: 'MAINTENANCE.ROLES.PLANNER',
  [AgentRole.Extractor]: 'MAINTENANCE.ROLES.EXTRACTOR',
  [AgentRole.Screener]: 'MAINTENANCE.ROLES.SCREENER',
};

export const RULE_LABEL_KEYS: Readonly<Record<SignalRule, string>> = {
  [SignalRule.Threshold]: 'MAINTENANCE.RULES.THRESHOLD',
  [SignalRule.Drift]: 'MAINTENANCE.RULES.DRIFT',
  [SignalRule.Trend]: 'MAINTENANCE.RULES.TREND',
  [SignalRule.Status]: 'MAINTENANCE.RULES.STATUS',
  [SignalRule.SilentMachine]: 'MAINTENANCE.RULES.SILENT_MACHINE',
  [SignalRule.UnknownMode]: 'MAINTENANCE.RULES.UNKNOWN_MODE',
};

export const TOOL_LABEL_KEYS: Readonly<Record<string, string>> = {
  search_manual: 'MAINTENANCE.TOOLS.SEARCH_MANUAL',
  get_metric_history: 'MAINTENANCE.TOOLS.GET_METRIC_HISTORY',
  get_signal_episodes: 'MAINTENANCE.TOOLS.GET_SIGNAL_EPISODES',
  list_open_work_orders: 'MAINTENANCE.TOOLS.LIST_OPEN_WORK_ORDERS',
  propose_work_order: 'MAINTENANCE.TOOLS.PROPOSE_WORK_ORDER',
  link_work_order: 'MAINTENANCE.TOOLS.LINK_WORK_ORDER',
  close_case: 'MAINTENANCE.TOOLS.CLOSE_CASE',
};

/** The sentence of one summary line: a translation key with its parameters, or a text that needs no translation. */
export type SummaryLine =
  { readonly key: string; readonly params: Readonly<Record<string, string | number>> } | { readonly text: string };

/** What the summary needs from the panel: the formatter of an instant in local time and the loaded translations. */
export interface SummaryContext {
  readonly formatTime: (instant: string) => string;
  readonly labels: Readonly<Record<string, string>>;
}

const SUMMARY_KEYS = {
  manual: 'MAINTENANCE.TOOL_SUMMARIES.MANUAL_PASSAGES',
  manualRepeated: 'MAINTENANCE.TOOL_SUMMARIES.MANUAL_PASSAGES_REPEATED',
  history: 'MAINTENANCE.TOOL_SUMMARIES.METRIC_HISTORY',
  signals: 'MAINTENANCE.TOOL_SUMMARIES.OPEN_SIGNALS',
  signal: 'MAINTENANCE.TOOL_SUMMARIES.OPEN_SIGNAL',
  workOrders: 'MAINTENANCE.TOOL_SUMMARIES.OPEN_WORK_ORDERS',
  proposed: 'MAINTENANCE.TOOL_SUMMARIES.PROPOSED_WORK_ORDER',
  linked: 'MAINTENANCE.TOOL_SUMMARIES.LINKED_WORK_ORDER',
  closed: 'MAINTENANCE.TOOL_SUMMARIES.CLOSE_REASON',
} as const;

/** Every key that the summaries translate besides the tool labels. */
export const SUMMARY_LABEL_KEYS: readonly string[] = [
  ...Object.values(TOOL_LABEL_KEYS),
  ...Object.values(RULE_LABEL_KEYS),
  ...Object.values(SEVERITY_LABEL_KEYS),
];

function parse(text: string): unknown {
  try {
    return JSON.parse(text);
  } catch {
    return undefined;
  }
}

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function listOf(value: unknown, field: string): readonly unknown[] | null {
  const items = asRecord(value)?.[field];
  return Array.isArray(items) ? items : null;
}

function textOf(value: unknown, field: string): string | null {
  const text = asRecord(value)?.[field];
  return typeof text === 'string' && text.trim() !== '' ? text : null;
}

function signalLine(episode: unknown, context: SummaryContext): SummaryLine | null {
  const record = asRecord(episode);
  if (record === null || record['state'] !== 'open') {
    return null;
  }
  const rule = textOf(record, 'rule') ?? '';
  const severity = textOf(record, 'severity') ?? '';
  const openedAt = textOf(record, 'openedAt');
  return {
    key: SUMMARY_KEYS.signal,
    params: {
      rule: labelOf(rule, RULE_LABEL_KEYS, context.labels),
      severity: labelOf(severity, SEVERITY_LABEL_KEYS, context.labels),
      subject: textOf(record, 'metricKey') ?? textOf(record, 'statusCode') ?? '',
      openedAt: openedAt === null ? '' : context.formatTime(openedAt),
    },
  };
}

function summarizeResult(call: CaseToolCall, context: SummaryContext): readonly SummaryLine[] {
  const result = parse(call.result);
  const args = parse(call.arguments);
  switch (call.toolName) {
    case 'search_manual': {
      const passages = listOf(result, 'passages');
      if (passages === null) {
        return [];
      }
      const repeated = passages.filter((passage) => asRecord(passage)?.['repeatOf'] !== undefined).length;
      return repeated === 0
        ? [{ key: SUMMARY_KEYS.manual, params: { count: passages.length } }]
        : [{ key: SUMMARY_KEYS.manualRepeated, params: { count: passages.length, repeated } }];
    }
    case 'get_metric_history': {
      const buckets = listOf(result, 'buckets');
      const from = textOf(result, 'from');
      const to = textOf(result, 'to');
      return buckets === null || from === null || to === null
        ? []
        : [
            {
              key: SUMMARY_KEYS.history,
              params: { count: buckets.length, from: context.formatTime(from), to: context.formatTime(to) },
            },
          ];
    }
    case 'get_signal_episodes': {
      const episodes = listOf(result, 'episodes');
      if (episodes === null) {
        return [];
      }
      const lines = episodes.map((episode) => signalLine(episode, context)).filter((line) => line !== null);
      return [{ key: SUMMARY_KEYS.signals, params: { count: lines.length } }, ...lines];
    }
    case 'list_open_work_orders': {
      const workOrders = listOf(result, 'workOrders');
      return workOrders === null ? [] : [{ key: SUMMARY_KEYS.workOrders, params: { count: workOrders.length } }];
    }
    case 'propose_work_order': {
      const title = textOf(args, 'title');
      return title === null ? [] : [{ key: SUMMARY_KEYS.proposed, params: { title } }];
    }
    case 'link_work_order': {
      const episodes = listOf(args, 'episodeIds');
      return episodes === null ? [] : [{ key: SUMMARY_KEYS.linked, params: { count: episodes.length } }];
    }
    case 'close_case': {
      const reason = textOf(args, 'reason');
      return reason === null ? [] : [{ key: SUMMARY_KEYS.closed, params: { reason } }];
    }
    default:
      return [];
  }
}

/**
 * The readable summary of one tool call, built from its stored arguments and result. A summary never holds an ID, an
 * internal field, or a UTC time. A call that did not succeed shows its failure text. A stored text that does not parse,
 * and a tool that the frontend does not know, give no summary.
 */
export function summarizeToolCall(call: CaseToolCall, context: SummaryContext): readonly SummaryLine[] {
  if (call.outcome !== ToolCallOutcome.Succeeded) {
    const failure = parse(call.result);
    return typeof failure === 'string' && failure.trim() !== '' ? [{ text: failure }] : [];
  }
  return summarizeResult(call, context);
}

/** Every key that the panel translates. The component streams them in one call. */
export const PANEL_LABEL_KEYS: readonly string[] = [
  ...Object.values(VERDICT_LABEL_KEYS),
  ...Object.values(RUN_ERROR_LABEL_KEYS),
  ...Object.values(SKIP_REASON_LABEL_KEYS),
  ...Object.values(OUTCOME_LABEL_KEYS),
  ...Object.values(ROLE_LABEL_KEYS),
  ...SUMMARY_LABEL_KEYS,
];

/**
 * The text of a closed value: its translated label, or the raw value when the value is unknown. A server that adds a
 * value before the frontend knows it still shows something readable.
 */
export function labelOf(
  value: string,
  keys: Readonly<Record<string, string>>,
  labels: Readonly<Record<string, string>>,
): string {
  const key = keys[value];
  return key === undefined ? value : (labels[key] ?? key);
}

/** The classes of the final text of a run: the model writes plain sentences on several lines, so the lines stay. */
export const FINAL_TEXT_CLASSES = 'm-0 min-w-0 break-words whitespace-pre-line';

/** Pretty-prints a JSON text with 2 spaces. A text that is not JSON stays as it is. */
export function formatPayload(text: string): string {
  try {
    return JSON.stringify(JSON.parse(text), null, 2);
  } catch {
    return text;
  }
}
