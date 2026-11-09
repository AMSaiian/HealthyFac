import { Component, computed, effect, inject, input, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';

import type { FilesPage } from '@files/file';
import { FileGrid } from '@files/list/file-grid/file-grid';
import { FilesGridStore } from '@files/list/files-grid.store';
import type { Machine } from '@machines/machine';

const EMPTY_PAGE: FilesPage = { items: [], totalCount: 0 };

/** The files of one machine. The router binds `machineId` from the route and `machine` from the resolver. */
@Component({
  imports: [ButtonDirective, FileGrid, RouterLink, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col gap-4 p-4' },
  selector: 'hf-machine-files-page',
  templateUrl: './machine-files-page.html',
})
export class MachineFilesPage {
  readonly machineId = input.required<string>();
  readonly machine = input.required<Machine>();

  protected readonly store = inject(FilesGridStore);

  protected readonly page = computed(() => (this.store.files.hasValue() ? this.store.files.value() : EMPTY_PAGE));

  constructor() {
    effect(() => {
      const machineId = this.machineId();
      untracked(() => this.store.setScope({ kind: 'machine', machineId }));
    });
  }
}
