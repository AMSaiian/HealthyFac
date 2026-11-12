import { Component, computed, effect, inject, untracked } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

import type { FilesPage } from '@files/file';
import { FileGrid } from '@files/list/file-grid/file-grid';
import { FilesGridStore } from '@files/list/files-grid.store';

const EMPTY_PAGE: FilesPage = { items: [], totalCount: 0 };

@Component({
  imports: [ButtonDirective, FileGrid, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-company-files-page',
  templateUrl: './company-files-page.html',
})
export class CompanyFilesPage {
  protected readonly store = inject(FilesGridStore);

  protected readonly page = computed(() => (this.store.files.hasValue() ? this.store.files.value() : EMPTY_PAGE));

  constructor() {
    effect(() => {
      untracked(() => this.store.setScope({ kind: 'company' }));
    });
  }
}
