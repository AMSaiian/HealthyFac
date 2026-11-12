import { Component, computed, inject, input, linkedSignal, output } from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';

import { isEmptyResult } from '@shared/grid/table/grid-display';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import type { ScheduleBar, ScheduleRow } from '@shared/schedule/schedule-row';
import type { Temporal } from '@shared/time/temporal';

import {
  axisOf,
  describeBar,
  gapsOf,
  hourLabelsOf,
  nowPercentOf,
  placeBars,
  skeletonRowCount,
  tooltipOf,
} from '@shared/schedule/schedule-board/schedule-board.utils';

/** Shows the day of several people as rows on one time axis: the work blocks, the work orders, and "now". */
@Component({
  imports: [EmptyState, Message, Skeleton, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-col' },
  selector: 'hf-schedule-board',
  styleUrl: './schedule-board.css',
  templateUrl: './schedule-board.html',
})
export class ScheduleBoard {
  readonly rows = input.required<readonly ScheduleRow[]>();
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
  readonly hasFailed = input(false);
  readonly date = input.required<Temporal.PlainDate>();
  readonly timeZoneId = input.required<string>();
  readonly now = input.required<Temporal.Instant>();

  readonly workOrderClick = output<string>();

  protected readonly axis = computed(() => axisOf(this.date(), this.timeZoneId()));
  protected readonly hours = computed(() => hourLabelsOf(this.axis(), this.timeZoneId()));
  protected readonly nowPercent = computed(() => nowPercentOf(this.now(), this.axis()));
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly showEmpty = computed(() =>
    isEmptyResult({ rows: this.rows(), isLoading: this.isLoading(), hasLoaded: this.hasLoaded() }),
  );
  private readonly _lastRowCount = linkedSignal<readonly ScheduleRow[], number>({
    source: this.rows,
    computation: (rows, previous) => (rows.length > 0 ? rows.length : (previous?.value ?? 0)),
  });
  protected readonly skeletonRowIndexes = computed(() =>
    Array.from({ length: skeletonRowCount(this._lastRowCount()) }, (_, index) => index),
  );

  private readonly _translate = inject(TranslateService);
  protected readonly layouts = computed(() =>
    this.rows().map((row) => {
      const { placed, laneCount } = placeBars(row.bars, this.axis());
      return {
        row,
        placed,
        gaps: gapsOf(row.blocks, this.axis()),
        lanes: Array.from({ length: laneCount }, (_, lane) => placed.filter((item) => item.lane === lane)),
      };
    }),
  );

  protected tooltip(bar: ScheduleBar): string {
    return tooltipOf(bar, this._translate.instant(bar.statusKey), this.timeZoneId());
  }

  protected describe(bar: ScheduleBar): string {
    return describeBar(bar, this.timeZoneId());
  }
}
