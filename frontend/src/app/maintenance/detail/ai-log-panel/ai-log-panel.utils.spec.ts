import { AgentRunErrorCode, AgentRunVerdict, type CaseToolCall, ToolCallOutcome } from '@maintenance/case';
import {
  FINAL_TEXT_CLASSES,
  formatPayload,
  labelOf,
  RUN_ERROR_LABEL_KEYS,
  summarizeToolCall,
  type SummaryContext,
  VERDICT_LABEL_KEYS,
} from '@maintenance/detail/ai-log-panel/ai-log-panel.utils';

const LABELS: Record<string, string> = {
  'MAINTENANCE.VERDICTS.DISMISS': 'Dismiss',
  'MAINTENANCE.VERDICTS.WATCH': 'Watch',
  'MAINTENANCE.VERDICTS.ESCALATE': 'Escalate',
  'MAINTENANCE.VERDICTS.SAFE': 'Safe',
  'MAINTENANCE.VERDICTS.UNSAFE': 'Unsafe',
  'MAINTENANCE.RUN_ERRORS.PROVIDER_UNAVAILABLE': 'Provider unavailable',
  'MAINTENANCE.RUN_ERRORS.UNEXPECTED_ERROR': 'Unexpected error',
  'MAINTENANCE.RUN_ERRORS.CASE_CHANGED': 'Case changed',
  'MAINTENANCE.RUN_ERRORS.NO_DECISION': 'No decision',
};

describe('ai log panel', { tags: ['Cases'] }, () => {
  it.each(Object.values(AgentRunVerdict))('labelOf_Verdict%s_GivesItsTranslatedLabel', (verdict) => {
    expect(labelOf(verdict, VERDICT_LABEL_KEYS, LABELS)).toBe(LABELS[VERDICT_LABEL_KEYS[verdict]]);
  });

  it.each(Object.values(AgentRunErrorCode))('labelOf_ErrorCode%s_GivesItsTranslatedLabel', (code) => {
    expect(labelOf(code, RUN_ERROR_LABEL_KEYS, LABELS)).toBe(LABELS[RUN_ERROR_LABEL_KEYS[code]]);
  });

  it.each([
    ['UnknownValue_GivesTheRawText', 'somethingNew', LABELS, 'somethingNew'],
    ['KnownValueBeforeTheTranslationLoads_GivesTheKey', 'watch', {}, 'MAINTENANCE.VERDICTS.WATCH'],
  ])('labelOf_%s', (_name, value, labels, expected) => {
    expect(labelOf(value, VERDICT_LABEL_KEYS, labels)).toBe(expected);
  });

  it.each([
    ['JsonText_GivesIndentedJson', '{"a":1}', '{\n  "a": 1\n}'],
    ['TextThatIsNotJson_StaysAsItIs', 'plain text', 'plain text'],
  ])('formatPayload_%s', (_name, text, expected) => {
    expect(formatPayload(text)).toBe(expected);
  });

  describe('summarizeToolCall', () => {
    const CONTEXT: SummaryContext = {
      formatTime: (instant) => `local(${instant})`,
      labels: {
        'MAINTENANCE.RULES.THRESHOLD': 'Threshold',
        'MAINTENANCE.RULES.STATUS': 'Status',
        'MAINTENANCE.SEVERITIES.ALARM': 'Alarm',
      },
    };
    const ID = 'v1_tl8GBg5fkAFyM6O0qPBxPPWo-Z1q1a9Z';

    function call(toolName: string, args: unknown, result: unknown, outcome = ToolCallOutcome.Succeeded): CaseToolCall {
      return {
        sequence: 1,
        toolName,
        outcome,
        arguments: JSON.stringify(args),
        result: JSON.stringify(result),
        durationMilliseconds: 5,
      };
    }

    const RECORDED: [string, CaseToolCall, unknown][] = [
      [
        'search_manual',
        call('search_manual', { query: 'bearing' }, { passages: [{ text: 'a' }, { text: 'b' }] }),
        [{ key: 'MAINTENANCE.TOOL_SUMMARIES.MANUAL_PASSAGES', params: { count: 2 } }],
      ],
      [
        'get_metric_history',
        call(
          'get_metric_history',
          { metricId: ID },
          { from: '2026-11-18T17:20:00Z', to: '2026-11-18T17:32:00Z', buckets: [{ avg: 1 }, { avg: 2 }, { avg: 3 }] },
        ),
        [
          {
            key: 'MAINTENANCE.TOOL_SUMMARIES.METRIC_HISTORY',
            params: { count: 3, from: 'local(2026-11-18T17:20:00Z)', to: 'local(2026-11-18T17:32:00Z)' },
          },
        ],
      ],
      [
        'get_signal_episodes',
        call(
          'get_signal_episodes',
          {},
          {
            episodes: [
              {
                rule: 'threshold',
                severity: 'alarm',
                state: 'open',
                metricKey: 'flow.rate',
                openedAt: '2026-11-18T17:27:41Z',
                episodeId: ID,
                companyId: ID,
                sequence: 1,
              },
              {
                rule: 'status',
                severity: 'alarm',
                state: 'resolved',
                statusCode: 'P-05',
                openedAt: '2026-11-18T17:00:00Z',
              },
            ],
          },
        ),
        [
          { key: 'MAINTENANCE.TOOL_SUMMARIES.OPEN_SIGNALS', params: { count: 1 } },
          {
            key: 'MAINTENANCE.TOOL_SUMMARIES.OPEN_SIGNAL',
            params: {
              rule: 'Threshold',
              severity: 'Alarm',
              subject: 'flow.rate',
              openedAt: 'local(2026-11-18T17:27:41Z)',
            },
          },
        ],
      ],
      [
        'list_open_work_orders',
        call('list_open_work_orders', {}, { workOrders: [{ id: ID }] }),
        [{ key: 'MAINTENANCE.TOOL_SUMMARIES.OPEN_WORK_ORDERS', params: { count: 1 } }],
      ],
      [
        'propose_work_order',
        call(
          'propose_work_order',
          { title: 'Replace the seal', episodeIds: [ID] },
          { actionId: ID, status: 'buffered' },
        ),
        [{ key: 'MAINTENANCE.TOOL_SUMMARIES.PROPOSED_WORK_ORDER', params: { title: 'Replace the seal' } }],
      ],
      [
        'link_work_order',
        call('link_work_order', { workOrderId: ID, episodeIds: [ID, ID] }, { actionId: ID, status: 'buffered' }),
        [{ key: 'MAINTENANCE.TOOL_SUMMARIES.LINKED_WORK_ORDER', params: { count: 2 } }],
      ],
      [
        'close_case',
        call('close_case', { reason: 'No further action' }, { actionId: ID, status: 'buffered' }),
        [{ key: 'MAINTENANCE.TOOL_SUMMARIES.CLOSE_REASON', params: { reason: 'No further action' } }],
      ],
    ];

    it('summarizeToolCall_SearchWithRepeatedPassages_CountsTheRepeats', () => {
      const search = call(
        'search_manual',
        { query: 'seal' },
        { passages: [{ text: 'a' }, { repeatOf: 1 }, { repeatOf: 2 }] },
      );

      expect(summarizeToolCall(search, CONTEXT)).toEqual([
        { key: 'MAINTENANCE.TOOL_SUMMARIES.MANUAL_PASSAGES_REPEATED', params: { count: 3, repeated: 2 } },
      ]);
    });

    it.each(RECORDED)('summarizeToolCall_%s_GivesItsSentence', (_tool, recorded, expected) => {
      expect(summarizeToolCall(recorded, CONTEXT)).toEqual(expected);
    });

    it.each(RECORDED)('summarizeToolCall_%s_ShowsNoIdAndNoUtcTime', (_tool, recorded) => {
      const text = JSON.stringify(summarizeToolCall(recorded, CONTEXT));

      expect(text).not.toContain(ID);
      expect(text).not.toContain('companyId');
      expect(text).not.toContain('sequence');
      expect(text).not.toMatch(/Z"/);
    });

    it('summarizeToolCall_RejectedCall_ShowsItsFailureText', () => {
      const rejected = call(
        'get_metric_history',
        { metricId: 'x' },
        'The arguments do not match the schema of the tool.',
        ToolCallOutcome.Rejected,
      );

      expect(summarizeToolCall(rejected, CONTEXT)).toEqual([
        { text: 'The arguments do not match the schema of the tool.' },
      ]);
    });

    it('summarizeToolCall_ResultThatDoesNotParse_GivesNoSummary', () => {
      const broken: CaseToolCall = { ...call('search_manual', {}, {}), result: '{not json' };

      expect(summarizeToolCall(broken, CONTEXT)).toEqual([]);
    });

    it.each([
      ['UnknownTool', 'new_tool', { ok: true }],
      ['ResultOfAnotherShape', 'search_manual', { passages: 'none' }],
    ])('summarizeToolCall_%s_GivesNoSummary', (_name, tool, result) => {
      expect(summarizeToolCall(call(tool, {}, result), CONTEXT)).toEqual([]);
    });
  });
});

describe('finalTextClasses', { tags: ['Cases'] }, () => {
  it('FINAL_TEXT_CLASSES_AreTheClassesOfTheTextOfARun_KeepTheLineBreaksOfTheText', () => {
    expect(FINAL_TEXT_CLASSES.split(' ')).toContain('whitespace-pre-line');
  });
});
