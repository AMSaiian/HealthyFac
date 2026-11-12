import type { ElementRef } from '@angular/core';
import { Component, computed, effect, input, linkedSignal, output, signal, viewChild } from '@angular/core';
import { disabled, type FieldTree, form, FormField, validate, validateStandardSchema } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import { isFieldInvalid } from '@shared/forms/field-state';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';

import { MetricValueKind } from '@machines/metrics/metric-definition';
import {
  KEY_TAKEN_ERROR_KEY,
  metricFormSchema,
  type MetricFormValue,
} from '@machines/metrics/metric-form/metric-form-schema';
import { DESCRIPTION_MAX_LENGTH } from '@machines/metrics/metric-limits';
import { KIND_OPTIONS, quantityKindKey, type UnitGroup } from '@machines/metrics/metric-form/metric-form.utils';
import { nextKey } from '@machines/metrics/metrics-page-store/metrics-page.store.utils';

const MAX_FRACTION_DIGITS = 6;

@Component({
  imports: [ButtonDirective, FormField, InputNumber, InputText, RichTextEditor, Select, Tag, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4' },
  selector: 'hf-metric-form',
  templateUrl: './metric-form.html',
})
export class MetricForm {
  readonly initialValue = input.required<MetricFormValue>();
  readonly isNew = input(false);
  readonly takenKeys = input<readonly string[]>([]);
  readonly unitGroups = input<readonly UnitGroup[]>([]);
  readonly isUnitsLoading = input(false);
  protected readonly showUnitsLoading = delayedLoading(this.isUnitsLoading);
  readonly isDoneDisabled = input(false);
  readonly doneDisabledReason = input<string | null>(null);
  readonly valueChange = output<MetricFormValue>();
  readonly validityChange = output<boolean>();
  readonly done = output<void>();
  readonly cancelled = output<void>();

  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  protected readonly kindOptions = KIND_OPTIONS;
  protected readonly quantityKindKey = quantityKindKey;
  protected readonly maxFractionDigits = MAX_FRACTION_DIGITS;
  protected readonly newState = signal('');
  private readonly _stateInput = viewChild.required<ElementRef<HTMLInputElement>>('stateInput');

  protected readonly model = linkedSignal<MetricFormValue>(() => ({
    ...this.initialValue(),
    states: [...this.initialValue().states],
  }));
  protected readonly metricForm = form(this.model, (path) => {
    validateStandardSchema(path, metricFormSchema);
    validate(path.key, (context) =>
      this.takenKeys().includes(context.value()) ? { kind: 'taken', message: KEY_TAKEN_ERROR_KEY } : undefined,
    );
    disabled(path.key, { when: () => !this.isNew() });
    disabled(path.valueKind, { when: () => !this.isNew() });
    disabled(path.unit, { when: () => !this.isNew() });
  });
  protected readonly unitGroupList = computed(() => [...this.unitGroups()]);
  protected readonly isNumeric = computed(() => this.model().valueKind === MetricValueKind.Numeric);
  protected readonly isStateSet = computed(() => this.model().valueKind === MetricValueKind.StateSet);
  protected readonly hasStateInput = computed(() => this.newState().trim() !== '');

  private _previousName = '';

  constructor() {
    effect(() => {
      this.valueChange.emit(this.model());
    });
    effect(() => {
      this.validityChange.emit(this.metricForm().valid());
    });
    effect(() => {
      const name = this.model().name;
      if (this.isNew() && name !== this._previousName) {
        const key = nextKey(this._previousName, this.model().key, name);
        this._previousName = name;
        if (key !== this.model().key) {
          this.model.update((value) => ({ ...value, key }));
        }
      }
    });
  }

  protected isShown(field: FieldTree<unknown>): boolean {
    return isFieldInvalid({ isInvalid: field().invalid(), isTouched: field().touched() });
  }

  protected addState(): void {
    const state = this.newState().trim();
    if (state !== '') {
      this.model.update((value) => ({ ...value, states: [...value.states, state] }));
      this.newState.set('');
      this._stateInput().nativeElement.value = '';
    }
  }

  protected removeState(index: number): void {
    this.model.update((value) => ({ ...value, states: value.states.filter((_, position) => position !== index) }));
  }
}
