import { Component, computed, effect, inject, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import { EmptyState } from '@shared/loading/empty-state/empty-state';

import { MetricDetail } from '@machines/metrics/metric-detail/metric-detail';
import { MetricForm } from '@machines/metrics/metric-form/metric-form';
import { toUnitGroups } from '@machines/metrics/metric-form/metric-form.utils';
import { MetricList } from '@machines/metrics/metric-list/metric-list';
import { MetricExampleDialog } from '@machines/metrics/metric-example-dialog/metric-example-dialog';
import { MetricsPageStore } from '@machines/metrics/metrics-page.store';

@Component({
  imports: [
    ButtonDirective,
    EmptyState,
    MetricDetail,
    MetricForm,
    MetricList,
    MetricExampleDialog,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-metrics-page',
  templateUrl: './metrics-page.html',
})
export class MetricsPage {
  /** The query parameter `draftAttemptId` (router input binding): the attempt whose metric drafts the page stages. */
  readonly draftAttemptId = input<string>();

  protected readonly store = inject(MetricsPageStore);

  protected readonly unitGroups = computed(() =>
    toUnitGroups(this.store.units.hasValue() ? this.store.units.value() : undefined),
  );
  protected readonly isLoading = computed(() => this.store.definitions.isLoading() && this.store.items().length === 0);
  protected readonly isEmpty = computed(
    () => this.store.definitions.hasValue() && this.store.items().length === 0 && this.store.edit() === null,
  );
  protected readonly selectedErrors = computed(() => this.store.itemErrors()[this.store.selectedId() ?? ''] ?? []);
  protected readonly changesDisabledReason = computed((): string | null => {
    if (this.store.edit() !== null) {
      return 'METRICS.DISABLED.FORM_OPEN';
    }
    return this.store.hasChanges() ? null : 'METRICS.DISABLED.NO_CHANGES';
  });
  protected readonly doneDisabledReason = computed((): string | null => {
    if (!this.store.isFormValid()) {
      return 'METRICS.FORM.INVALID';
    }
    return this.store.isFormChanged() ? null : 'METRICS.FORM.NO_CHANGES';
  });

  constructor() {
    effect(() => {
      this.store.loadDrafts(this.draftAttemptId() ?? null);
    });
  }
}
