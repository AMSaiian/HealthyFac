import { DOCUMENT } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import type { ChartData, ChartOptions, TooltipItem } from 'chart.js';
import { ChartModule } from 'primeng/chart';
import { TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';

import { cssColor } from '@shared/theme/css-color';

import {
  barTotal,
  colorTokensOf,
  durationParts,
  type TimeRow,
} from '@machines/signals/time-breakdown/time-breakdown.utils';

const DURATION_KEY = 'SIGNALS.TIME.DURATION';

/**
 * Shows how a range splits over phases or states: one stacked bar, and a table with the duration and the share of
 * each row. It reads no store: the page passes the rows.
 */
@Component({
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-2' },
  imports: [ChartModule, TableModule, Tag, TranslatePipe],
  selector: 'hf-time-breakdown',
  templateUrl: './time-breakdown.html',
})
export class TimeBreakdown {
  readonly rows = input.required<readonly TimeRow[]>();
  /** The theme of the page. A change draws the bar again with the colors of the new theme. */
  readonly isDark = input(false);

  protected readonly rowList = computed(() => [...this.rows()]);

  private readonly _document = inject(DOCUMENT);
  private readonly _translate = inject(TranslateService);
  private readonly _durations = toSignal(this._translate.stream(DURATION_KEY), { initialValue: DURATION_KEY });

  protected readonly data = computed((): ChartData<'bar'> => {
    this.isDark();
    const rows = this.rows();
    const tokens = colorTokensOf(rows);
    return {
      labels: [''],
      datasets: rows.flatMap((row, index) =>
        row.seconds > 0
          ? [
              {
                label: row.label,
                data: [row.seconds],
                backgroundColor: cssColor(this._document, tokens[index]),
                borderWidth: 0,
              },
            ]
          : [],
      ),
    };
  });

  protected readonly options = computed((): ChartOptions<'bar'> => {
    this.isDark();
    const muted = cssColor(this._document, '--p-text-muted-color');
    return {
      indexAxis: 'y',
      responsive: true,
      maintainAspectRatio: false,
      animation: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            label: (item: TooltipItem<'bar'>) => `${item.dataset.label ?? ''}: ${this.durationText(Number(item.raw))}`,
          },
        },
      },
      scales: {
        x: { stacked: true, display: false, min: 0, max: barTotal(this.rows()) },
        y: { stacked: true, display: false, ticks: { color: muted } },
      },
    };
  });

  /** The color of the swatch of a row in the table. */
  protected swatch(index: number): string {
    this.isDark();
    return cssColor(this._document, colorTokensOf(this.rows())[index]);
  }

  protected durationText(seconds: number): string {
    this._durations();
    return this._translate.instant(DURATION_KEY, durationParts(seconds)) as string;
  }
}
