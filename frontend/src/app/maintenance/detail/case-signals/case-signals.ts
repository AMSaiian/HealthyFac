import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Paginator, type PaginatorState } from 'primeng/paginator';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { CLOCK } from '@shared/time/clock';
import { PAGE_SIZES } from '@shared/grid/requests/grid-limits';
import { LocalDateTimePipe } from '@shared/time/local-date-time.pipe';

import { type CaseSignal, SignalCoverage, SignalSeverity } from '@maintenance/case';
import { labelOf, RULE_LABEL_KEYS } from '@maintenance/detail/ai-log-panel/ai-log-panel.utils';
import {
  chartQueryOf,
  signalComparison,
  signalDurationSeconds,
} from '@maintenance/detail/case-signals/case-signals.utils';
import {
  type ChartQuery,
  COVERAGE_LABEL_KEYS,
  groupKeyOf,
  signalSubject,
} from '@maintenance/detail/case-signals/case-signals.utils';
import { SEVERITY_LABEL_KEYS } from '@maintenance/list/cases-grid/cases-grid.utils';

const SECONDS_PER_MINUTE = 60;
const MINUTES_PER_HOUR = 60;

/** The paged signals of a case. It reads no store: the page passes the rows and takes the page events. */
@Component({
  imports: [
    ButtonDirective,
    DecimalPipe,
    LocalDateTimePipe,
    Paginator,
    RouterLink,
    TableModule,
    Tag,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-w-0 flex-col gap-2' },
  selector: 'hf-case-signals',
  templateUrl: './case-signals.html',
})
export class CaseSignals {
  readonly signals = input.required<readonly CaseSignal[]>();
  readonly machineId = input.required<string>();
  readonly totalCount = input.required<number>();
  readonly offset = input.required<number>();
  readonly limit = input.required<number>();
  readonly pageChange = output<{ offset: number; limit: number }>();

  protected readonly pageSizes = [...PAGE_SIZES];
  protected readonly signalSubject = signalSubject;
  protected readonly signalComparison = signalComparison;
  protected readonly rows = computed(() => [...this.signals()]);

  private readonly _clock = inject(CLOCK);
  private readonly _translate = inject(TranslateService);
  private readonly _labels = toSignal(
    this._translate.stream([
      ...Object.values(SEVERITY_LABEL_KEYS),
      ...Object.values(RULE_LABEL_KEYS),
      ...Object.values(COVERAGE_LABEL_KEYS),
      'MAINTENANCE.SIGNAL_TABLE.DURATION',
    ]),
    { initialValue: {} as Record<string, string> },
  );

  protected severityLabel(severity: SignalSeverity): string {
    return labelOf(severity, SEVERITY_LABEL_KEYS, this._labels());
  }

  protected severityTag(severity: SignalSeverity): 'danger' | 'warn' {
    return severity === SignalSeverity.Alarm ? 'danger' : 'warn';
  }

  protected ruleLabel(rule: string): string {
    return labelOf(rule, RULE_LABEL_KEYS, this._labels());
  }

  protected coverageLabel(signal: CaseSignal): string {
    return labelOf(groupKeyOf(signal), COVERAGE_LABEL_KEYS, this._labels());
  }

  protected coverageTag(coverage: SignalCoverage): 'info' | 'secondary' | 'success' {
    switch (coverage) {
      case SignalCoverage.Drafted:
      case SignalCoverage.Linked:
        return 'success';
      case SignalCoverage.New:
        return 'info';
      default:
        return 'secondary';
    }
  }

  /** The span of a resolved signal as hours and minutes; an open signal has none. */
  protected durationText(signal: CaseSignal): string | null {
    const seconds = signalDurationSeconds(signal);
    if (seconds === null) {
      return null;
    }
    const totalMinutes = Math.round(seconds / SECONDS_PER_MINUTE);
    return this._translate.instant('MAINTENANCE.SIGNAL_TABLE.DURATION', {
      hours: Math.floor(totalMinutes / MINUTES_PER_HOUR),
      minutes: totalMinutes % MINUTES_PER_HOUR,
    }) as string;
  }

  protected chartQuery(signal: CaseSignal): ChartQuery {
    return chartQueryOf(signal, this._clock.now().epochMilliseconds);
  }

  protected onPage(event: PaginatorState): void {
    this.pageChange.emit({ offset: event.first ?? 0, limit: event.rows ?? this.limit() });
  }
}
