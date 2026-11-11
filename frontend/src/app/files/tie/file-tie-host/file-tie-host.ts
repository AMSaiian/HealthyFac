import { Component, effect, inject, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { Select } from 'primeng/select';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';

import { FilesApi } from '@files/files-api';
import { FileTieStore } from '@files/tie/file-tie.store';

@Component({
  imports: [ActionDialog, FormsModule, Select, TranslatePipe],
  host: { class: 'contents' },
  providers: [FilesApi, FileTieStore],
  selector: 'hf-file-tie-host',
  templateUrl: './file-tie-host.html',
})
export class FileTieHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(FileTieStore);

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }

  protected onSave(): void {
    if (this.store.saveDisabledReason() === null) {
      this.store.save();
    }
  }
}
