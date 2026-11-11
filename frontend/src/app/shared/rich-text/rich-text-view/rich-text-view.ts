import { Component, computed, inject, input } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslateService } from '@ngx-translate/core';

import { toDisplayHtml } from '@shared/rich-text/file-image/rich-text-images';

const EMPTY_LINKS: ReadonlyMap<string, string> = new Map();

/** Shows the stored HTML of a rich text. An image gets its link from `links`; one without a link shows its alt text. */
@Component({
  host: { class: 'hf-rich-text block min-w-0 break-words' },
  selector: 'hf-rich-text-view',
  templateUrl: './rich-text-view.html',
})
export class RichTextView {
  readonly html = input.required<string>();
  readonly links = input<ReadonlyMap<string, string>>(EMPTY_LINKS);

  private readonly _missingText = toSignal(inject(TranslateService).stream('RICH_TEXT.IMAGE_MISSING'), {
    initialValue: '',
  });

  protected readonly displayHtml = computed(() => toDisplayHtml(this.html(), this.links(), this._missingText()));
}
