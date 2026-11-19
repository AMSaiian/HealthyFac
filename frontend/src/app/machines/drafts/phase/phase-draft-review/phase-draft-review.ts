import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { InputNumber } from 'primeng/inputnumber';
import { Message } from 'primeng/message';
import { Tag } from 'primeng/tag';

import { RichTextView } from '@shared/rich-text/rich-text-view/rich-text-view';

import type { PhaseDraftDetail } from '@machines/drafts/draft';
import type { MetricDefinition } from '@machines/metrics/metric-definition';
import { PhaseForm } from '@machines/phases/phase-form/phase-form';
import type { PhaseFormValue } from '@machines/phases/phase-form/phase-form-schema';
import type { ConditionSourceOption } from '@machines/phases/phase-form/phase-form.utils';
import { toConditionRows, toLimitRows } from '@machines/drafts/phase/phase-draft-review/phase-draft-review.utils';
import type { PhaseSourceNames } from '@machines/drafts/phase/phase-draft-review-store/phase-draft-review.store.utils';
import { Divider } from 'primeng/divider';

/** Shows a phase draft: its values, its conditions, its limit rows, and the target state of each source. */
@Component({
  imports: [Divider, FormsModule, InputNumber, Message, PhaseForm, RichTextView, Tag, TranslatePipe],
  host: { class: 'flex min-w-0 flex-col gap-4' },
  selector: 'hf-phase-draft-review',
  templateUrl: './phase-draft-review.html',
})
export class PhaseDraftReview {
  readonly draft = input.required<PhaseDraftDetail>();
  readonly names = input.required<PhaseSourceNames>();
  /** The translation key of the reason that the draft cannot apply, or `null`. */
  readonly blockKey = input<string | null>(null);
  /** True when the phase form edits the draft. A draft that waits for a metric or status draft is read-only. */
  readonly isEditable = input(false);
  readonly initialValue = input<PhaseFormValue | null>(null);
  readonly sourceOptions = input<readonly ConditionSourceOption[]>([]);
  readonly metrics = input<readonly MetricDefinition[]>([]);
  readonly priority = input<number | null>(null);
  readonly valueChange = output<PhaseFormValue>();
  readonly validityChange = output<boolean>();
  readonly priorityChange = output<number | null>();

  protected readonly conditions = computed(() => toConditionRows(this.draft(), this.names()));
  protected readonly limits = computed(() => toLimitRows(this.draft(), this.names()));
}
