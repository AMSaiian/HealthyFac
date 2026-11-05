import { Component, computed, input, model, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Editor } from 'primeng/editor';

import { richTextLength } from '@shared/rich-text/rich-text-length';

const EMPTY_EDITOR_HTML = '<p><br></p>';

@Component({
  // eslint-disable-next-line @typescript-eslint/no-deprecated -- PrimeNG Editor stays until a free replacement is chosen (user decision 2026-11-04).
  imports: [Editor, FormsModule, TranslatePipe],
  host: { class: 'flex flex-col gap-1' },
  selector: 'hf-rich-text-editor',
  templateUrl: './rich-text-editor.html',
})
export class RichTextEditor implements FormValueControl<string | null> {
  readonly value = model<string | null>(null);
  readonly characterLimit = input<number | undefined>(undefined);
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly touched = input(false);
  readonly placeholder = input('');
  readonly touch = output<void>();

  protected readonly isInvalidShown = computed(() => this.invalid() && this.touched());
  protected readonly length = computed(() => richTextLength(this.value()));
  protected readonly isTooLong = computed(() => this.length() > (this.characterLimit() ?? Infinity));

  protected onEditorChange(html: string | null): void {
    this.value.set(html === null || html === '' || html === EMPTY_EDITOR_HTML ? null : html);
  }
}
