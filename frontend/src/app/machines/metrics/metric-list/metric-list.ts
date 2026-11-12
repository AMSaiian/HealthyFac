import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Skeleton } from 'primeng/skeleton';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { skeletonRows } from '@shared/grid/table/grid-display';
import { delayedLoading } from '@shared/loading/delayed-loading';

import type { MetricMarker } from '@machines/metrics/metrics-page-store/metrics-page.store.utils';
import { markerOf, type MetricItem } from '@machines/metrics/metrics-page-store/metrics-page.store.utils';
import { MARKER_LABEL_KEYS, MARKER_SEVERITIES } from '@machines/metrics/metric-list/metric-list.utils';

const SKELETON_ROW_COUNT = 6;

@Component({
  imports: [ButtonDirective, Skeleton, Tag, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 w-80 flex-none flex-col gap-2' },
  selector: 'hf-metric-list',
  templateUrl: './metric-list.html',
})
export class MetricList {
  readonly items = input.required<readonly MetricItem[]>();
  readonly selectedId = input<string | null>(null);
  readonly itemErrors = input<Readonly<Record<string, readonly string[]>>>({});
  readonly canWrite = input(false);
  readonly isLoading = input(false);
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  readonly selected = output<string>();
  readonly addRequested = output<void>();

  protected readonly skeletons = skeletonRows(SKELETON_ROW_COUNT);
  protected readonly markerLabelKeys = MARKER_LABEL_KEYS;
  protected readonly markerSeverities = MARKER_SEVERITIES;

  protected markerOf(item: MetricItem): MetricMarker | null {
    return markerOf(item, (this.itemErrors()[item.localId] ?? []).length > 0);
  }
}
