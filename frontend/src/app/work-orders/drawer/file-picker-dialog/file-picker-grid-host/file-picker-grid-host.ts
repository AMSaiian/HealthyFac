import { Component, computed, effect, inject, input, output, untracked } from '@angular/core';

import type { FilesPage, FilesScope } from '@files/file';
import { FileGrid } from '@files/list/file-grid/file-grid';
import { FilesApi } from '@files/files-api';
import { FilesGridStore } from '@files/list/files-grid.store';

const EMPTY_PAGE: FilesPage = { items: [], totalCount: 0 };

/** One file grid of the picker (company or machine files) with a selection column. It owns its own grid state. */
@Component({
  imports: [FileGrid],
  host: { class: 'flex h-96 min-h-0 min-w-0 flex-col' },
  providers: [FilesApi, FilesGridStore],
  selector: 'hf-file-picker-grid-host',
  templateUrl: './file-picker-grid-host.html',
})
export class FilePickerGridHost {
  readonly scope = input.required<FilesScope>();
  readonly selectedIds = input.required<readonly string[]>();
  readonly lockedIds = input.required<readonly string[]>();

  readonly selectionToggled = output<{ id: string; isSelected: boolean }>();

  protected readonly store = inject(FilesGridStore);

  protected readonly page = computed(() => (this.store.files.hasValue() ? this.store.files.value() : EMPTY_PAGE));

  constructor() {
    effect(() => {
      const scope = this.scope();
      untracked(() => this.store.setScope(scope));
    });
  }
}
