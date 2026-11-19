import { Component, computed, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Divider } from 'primeng/divider';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { EMPTY_CELL } from '@shared/grid/table/grid-display';
import { RichTextView } from '@shared/rich-text/rich-text-view/rich-text-view';

import type { MetricDefinition } from '@machines/metrics/metric-definition';
import { ConditionSourceKind, PhaseConditionOperator } from '@machines/phases/operating-phase';
import { metricName, numberText, sourceLabel, statesText } from '@machines/phases/phase-details/phase-details.utils';
import type { PhaseFormValue } from '@machines/phases/phase-form/phase-form-schema';
import {
  type ConditionSourceOption,
  hasNoLimitValues,
  kindOfSource,
  maskedMetricNames,
  OPERATOR_LABEL_KEYS,
  operatorSign,
} from '@machines/phases/phase-form/phase-form.utils';

/** The read-only view of an operating phase: the same sections as `PhaseForm`, as static text. */
@Component({
  imports: [ButtonDirective, Divider, Message, RichTextView, Tag, Tooltip, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-phase-details',
  templateUrl: './phase-details.html',
})
export class PhaseDetails {
  readonly value = input.required<PhaseFormValue>();
  readonly sourceOptions = input.required<readonly ConditionSourceOption[]>();
  readonly metrics = input.required<readonly MetricDefinition[]>();
  readonly descriptionLinks = input<ReadonlyMap<string, string>>(new Map());

  protected readonly emptyCell = EMPTY_CELL;
  protected readonly ConditionSourceKind = ConditionSourceKind;
  protected readonly PhaseConditionOperator = PhaseConditionOperator;
  protected readonly operatorLabelKeys = OPERATOR_LABEL_KEYS;
  protected readonly operatorSign = operatorSign;
  protected readonly hasNoLimitValues = hasNoLimitValues;
  protected readonly kindOfSource = kindOfSource;
  protected readonly metricName = metricName;
  protected readonly numberText = numberText;
  protected readonly sourceLabel = sourceLabel;
  protected readonly statesText = statesText;

  protected readonly maskedNames = computed(() =>
    maskedMetricNames(this.value().conditions, this.metrics()).join(', '),
  );
}
