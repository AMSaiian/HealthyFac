import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Checkbox } from 'primeng/checkbox';
import { InputText } from 'primeng/inputtext';

import { RichTextEditor } from '@shared/rich-text/rich-text-editor/rich-text-editor';
import { RichTextView } from '@shared/rich-text/rich-text-view/rich-text-view';

import { CompareField, type CompareRow } from '@machines/drafts/compare/draft-compare-store/draft-compare.store.utils';
import { DESCRIPTION_MAX_LENGTH } from '@machines/machine-limits';

const LABEL_KEYS: Readonly<Record<CompareField, string>> = {
  [CompareField.Name]: 'DRAFTS.COMPARE.FIELDS.NAME',
  [CompareField.SerialNumber]: 'DRAFTS.COMPARE.FIELDS.SERIAL_NUMBER',
  [CompareField.Description]: 'DRAFTS.COMPARE.FIELDS.DESCRIPTION',
};

@Component({
  imports: [Checkbox, FormsModule, InputText, RichTextEditor, RichTextView, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-field-compare-list',
  templateUrl: './field-compare-list.html',
})
export class FieldCompareList {
  readonly rows = input.required<readonly CompareRow[]>();

  readonly takeChange = output<{ field: CompareField; isTaken: boolean }>();
  readonly editChange = output<{ field: CompareField; value: string | null }>();

  protected readonly CompareField = CompareField;
  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;

  protected labelKey(field: CompareField): string {
    return LABEL_KEYS[field];
  }
}
