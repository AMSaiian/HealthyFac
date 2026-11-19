import { Component, inject, Injector, input, output, runInInjectionContext } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Accordion, AccordionContent, AccordionHeader, AccordionPanel } from 'primeng/accordion';
import { Paginator, type PaginatorState } from 'primeng/paginator';
import { Panel } from 'primeng/panel';
import { Tag } from 'primeng/tag';

import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import { AgentRole, AgentRunState, type CaseRun, type CaseToolCall, ToolCallOutcome } from '@maintenance/case';
import {
  FINAL_TEXT_CLASSES,
  formatPayload,
  labelOf,
  OUTCOME_LABEL_KEYS,
  PANEL_LABEL_KEYS,
  ROLE_LABEL_KEYS,
  RUN_ERROR_LABEL_KEYS,
  SKIP_REASON_LABEL_KEYS,
  summarizeToolCall,
  TOOL_LABEL_KEYS,
  VERDICT_LABEL_KEYS,
} from '@maintenance/detail/ai-log-panel/ai-log-panel.utils';
import { RUN_STATE_LABEL_KEYS } from '@maintenance/list/cases-grid/cases-grid.utils';

/** The AI log of a case: each run with its verdict, its error, and its tool calls. It reads no store. */
@Component({
  imports: [
    Accordion,
    AccordionContent,
    AccordionHeader,
    AccordionPanel,
    LocalDateTimePipe,
    Paginator,
    Panel,
    Tag,
    TranslatePipe,
  ],
  host: { class: 'flex min-w-0 flex-col gap-2' },
  selector: 'hf-ai-log-panel',
  templateUrl: './ai-log-panel.html',
})
export class AiLogPanel {
  readonly runs = input.required<readonly CaseRun[]>();
  /** The tool calls of the runs that the user opened, by run ID. A run that is not in it still loads. */
  readonly toolCalls = input.required<Readonly<Record<string, readonly CaseToolCall[]>>>();
  readonly totalCount = input.required<number>();
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly runOpened = output<string>();
  readonly pageChange = output<{ offset: number; limit: number }>();

  protected readonly pageSizes = [...PAGE_SIZES];

  protected readonly finalTextClasses = FINAL_TEXT_CLASSES;
  protected readonly formatPayload = formatPayload;
  protected readonly ToolCallOutcome = ToolCallOutcome;
  protected readonly AgentRunState = AgentRunState;
  protected readonly AgentRole = AgentRole;

  private readonly _translate = inject(TranslateService);
  private readonly _injector = inject(Injector);
  private _timePipe: LocalDateTimePipe | null = null;
  private readonly _opened = new Set<string>();
  private readonly _labels = toSignal(
    this._translate.stream([...PANEL_LABEL_KEYS, ...Object.values(RUN_STATE_LABEL_KEYS)]),
    { initialValue: {} as Record<string, string> },
  );

  /** Tells the page which run the user opened, so it loads the tool calls of that run once. */
  protected onValueChange(value: unknown): void {
    const values = Array.isArray(value) ? (value as unknown[]) : [value];
    for (const id of values) {
      if (typeof id === 'string' && !this._opened.has(id)) {
        this._opened.add(id);
        this.runOpened.emit(id);
      }
    }
  }

  protected onPage(event: PaginatorState): void {
    this.pageChange.emit({ offset: event.first ?? 0, limit: event.rows ?? this.limit() });
  }

  protected verdictLabel(verdict: string): string {
    return labelOf(verdict, VERDICT_LABEL_KEYS, this._labels());
  }

  protected errorLabel(errorCode: string): string {
    return labelOf(errorCode, RUN_ERROR_LABEL_KEYS, this._labels());
  }

  protected outcomeLabel(outcome: string): string {
    return labelOf(outcome, OUTCOME_LABEL_KEYS, this._labels());
  }

  protected runStateLabel(state: string): string {
    return labelOf(state, RUN_STATE_LABEL_KEYS, this._labels());
  }

  protected skipReasonLabel(reason: string): string {
    return labelOf(reason, SKIP_REASON_LABEL_KEYS, this._labels());
  }

  protected toolLabel(toolName: string): string {
    return labelOf(toolName, TOOL_LABEL_KEYS, this._labels());
  }

  /** The readable lines of a tool call. The call to `_labels` makes the view follow a language change. */
  protected summaryLines(call: CaseToolCall): readonly string[] {
    const labels = this._labels();
    return summarizeToolCall(call, { formatTime: (instant) => this._time().transform(instant), labels }).map((line) =>
      'text' in line ? line.text : this._translate.instant(line.key, line.params),
    );
  }

  /** The formatter of local times. It is created on first use, so a run without a tool call never needs it. */
  private _time(): LocalDateTimePipe {
    this._timePipe ??= runInInjectionContext(this._injector, () => new LocalDateTimePipe());
    return this._timePipe;
  }

  protected roleLabel(role: string): string {
    return labelOf(role, ROLE_LABEL_KEYS, this._labels());
  }
}
