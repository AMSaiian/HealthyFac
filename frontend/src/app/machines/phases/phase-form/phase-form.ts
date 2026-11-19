import { Component, computed, effect, input, linkedSignal, output } from '@angular/core';
import { type FieldTree, form, FormField, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Divider } from 'primeng/divider';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { Select } from 'primeng/select';
import { Tooltip } from 'primeng/tooltip';

import { isFieldInvalid } from '@shared/forms/field-state';
import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';

import type { MetricDefinition } from '@machines/metrics/metric-definition';
import { ConditionSourceKind, PhaseConditionOperator } from '@machines/phases/operating-phase';
import { PHASE_DESCRIPTION_MAX_LENGTH } from '@machines/phases/operating-phase-limits';
import { phaseFormSchema, type PhaseFormValue } from '@machines/phases/phase-form/phase-form-schema';
import {
  type ConditionSourceOption,
  hasNoLimitValues,
  kindOfSource,
  limitMetricOptions,
  maskedMetricNames,
  newCondition,
  newLimit,
  normalizedCondition,
  OPERATOR_LABEL_KEYS,
  operatorsFor,
  operatorSign,
} from '@machines/phases/phase-form/phase-form.utils';

@Component({
  imports: [
    Divider,
    ButtonDirective,
    Checkbox,
    FormField,
    InputNumber,
    InputText,
    Message,
    RichTextEditor,
    Select,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-phase-form',
  templateUrl: './phase-form.html',
})
export class PhaseForm {
  readonly initialValue = input.required<PhaseFormValue>();
  readonly sourceOptions = input.required<readonly ConditionSourceOption[]>();
  readonly metrics = input.required<readonly MetricDefinition[]>();
  readonly valueChange = output<PhaseFormValue>();
  readonly validityChange = output<boolean>();

  protected readonly descriptionMaxLength = PHASE_DESCRIPTION_MAX_LENGTH;
  protected readonly PhaseConditionOperator = PhaseConditionOperator;
  protected readonly ConditionSourceKind = ConditionSourceKind;

  protected readonly model = linkedSignal<PhaseFormValue>(() => structuredClone(this.initialValue()));
  protected readonly phaseForm = form(this.model, (path) => {
    validateStandardSchema(path, phaseFormSchema);
  });

  protected readonly maskedNames = computed(() =>
    maskedMetricNames(this.model().conditions, this.metrics()).join(', '),
  );
  protected readonly availableLimitMetrics = computed(() =>
    limitMetricOptions(this.metrics(), this.model().conditions),
  );
  protected readonly canAddLimit = computed(() => this.availableLimitMetrics().length > 0);
  protected readonly metricList = computed(() => [...this.metrics()]);
  protected readonly sourceOptionList = computed(() => [...this.sourceOptions()]);

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.phaseForm().valid());
    });
    effect(() => {
      const options = this.sourceOptions();
      const conditions = this.model().conditions;
      const normalized = conditions.map((condition) => normalizedCondition(condition, options));
      if (normalized.some((condition, index) => condition !== conditions[index])) {
        this.model.update((value) => ({ ...value, conditions: normalized }));
      }
    });
  }

  protected hasNoLimits(index: number): boolean {
    const limit = this.model().limits[index];
    return limit !== undefined && hasNoLimitValues(limit);
  }

  protected operatorSign(operator: PhaseConditionOperator): string {
    return operatorSign(operator);
  }

  protected operatorLabel(operator: PhaseConditionOperator): string {
    return OPERATOR_LABEL_KEYS[operator];
  }

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }

  protected kindAt(index: number): ConditionSourceKind | null {
    return kindOfSource(this.model().conditions[index]?.sourceId ?? '', this.sourceOptions());
  }

  protected operatorsAt(index: number): PhaseConditionOperator[] {
    const kind = this.kindAt(index);
    return kind === null ? [] : [...operatorsFor(kind)];
  }

  protected statesAt(index: number): string[] {
    const sourceId = this.model().conditions[index]?.sourceId;
    return [...(this.metrics().find((metric) => metric.id === sourceId)?.states ?? [])];
  }

  protected addCondition(): void {
    this.model.update((value) => ({
      ...value,
      conditions: [...value.conditions, newCondition(this.sourceOptions()[0] ?? null)],
    }));
  }

  protected removeCondition(index: number): void {
    this.model.update((value) => ({ ...value, conditions: value.conditions.filter((_, i) => i !== index) }));
  }

  protected addLimit(): void {
    const metric = this.availableLimitMetrics()[0];
    if (metric !== undefined) {
      this.model.update((value) => ({ ...value, limits: [...value.limits, newLimit(metric)] }));
    }
  }

  protected removeLimit(index: number): void {
    this.model.update((value) => ({ ...value, limits: value.limits.filter((_, i) => i !== index) }));
  }
}
