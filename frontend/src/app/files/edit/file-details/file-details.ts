import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

import { FileTypeTag } from '@shared/files/file-type-tag/file-type-tag';
import { EMPTY_CELL } from '@shared/grid/table/grid-display';
import { RichTextView } from '@shared/rich-text/rich-text-view/rich-text-view';

import type { FileEditFormValue } from '@files/edit/file-edit-form/file-edit-form-schema';
import { Divider } from 'primeng/divider';

/** The read-only view of a file: the same fields as `FileEditForm`, as static text. */
@Component({
  imports: [Divider, FileTypeTag, RichTextView, TranslatePipe],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-file-details',
  templateUrl: './file-details.html',
})
export class FileDetails {
  readonly value = input.required<FileEditFormValue>();
  readonly extension = input.required<string>();
  readonly descriptionLinks = input<ReadonlyMap<string, string>>(new Map());

  protected readonly emptyCell = EMPTY_CELL;
}
