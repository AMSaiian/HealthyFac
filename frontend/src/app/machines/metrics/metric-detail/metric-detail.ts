import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';

import { EMPTY_CELL } from '@shared/grid/table/grid-display';

import { MetricValueKind } from '@machines/metrics/metric-definition';
import { type MetricItem } from '@machines/metrics/metrics-page.store.utils';
import { findUnitOption, KIND_LABEL_KEYS, type UnitGroup } from '@machines/metrics/metric-form/metric-form.utils';

@Component({
  imports: [ButtonDirective, Message, Tag, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 overflow-y-auto' },
  selector: 'hf-metric-detail',
  templateUrl: './metric-detail.html',
})
export class MetricDetail {
  readonly item = input.required<MetricItem>();
  readonly errors = input<readonly string[]>([]);
  readonly canWrite = input(false);
  readonly unitGroups = input<readonly UnitGroup[]>([]);
  readonly editRequested = output<void>();
  readonly removeRequested = output<void>();
  readonly restoreRequested = output<void>();

  protected readonly emptyCell = EMPTY_CELL;
  protected readonly kindLabelKeys = KIND_LABEL_KEYS;
  protected readonly isNumeric = computed(() => this.item().value.valueKind === MetricValueKind.Numeric);
  protected readonly isStateSet = computed(() => this.item().value.valueKind === MetricValueKind.StateSet);
  protected readonly unitOption = computed(() => findUnitOption(this.unitGroups(), this.item().value.unit));
  protected readonly range = computed(() => {
    const { min, max } = this.item().value;
    return `${min ?? EMPTY_CELL} … ${max ?? EMPTY_CELL}`;
  });
}
