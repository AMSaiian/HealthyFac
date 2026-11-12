import { UpperCasePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { Tag } from 'primeng/tag';

import { fileTypeClass } from './file-type-tag.utils';

@Component({
  imports: [Tag, UpperCasePipe],
  host: { class: 'inline-flex' },
  selector: 'hf-file-type-tag',
  templateUrl: './file-type-tag.html',
})
export class FileTypeTag {
  readonly extension = input.required<string>();

  protected readonly typeClass = computed(() => fileTypeClass(this.extension()));
}
