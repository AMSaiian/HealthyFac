import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { EMPTY_CELL } from '@shared/grid/table/grid-display';
import { RichTextView } from '@shared/rich-text/rich-text-view/rich-text-view';
import { SEVERITY_LABEL_KEYS } from '@machines/statuses/status-definition.utils';
import type { StatusDefinitionFormValue } from '@machines/statuses/drawer/status-definition-form/status-definition-form-schema';
import { Divider } from 'primeng/divider';

/** The read-only view of a status definition: the same fields as `StatusDefinitionForm`, as static text. */
@Component({
  imports: [Divider, RichTextView, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-status-definition-details',
  templateUrl: './status-definition-details.html',
})
export class StatusDefinitionDetails {
  readonly value = input.required<StatusDefinitionFormValue>();
  readonly descriptionLinks = input<ReadonlyMap<string, string>>(new Map());

  protected readonly emptyCell = EMPTY_CELL;
  protected readonly severityLabelKeys = SEVERITY_LABEL_KEYS;
}
