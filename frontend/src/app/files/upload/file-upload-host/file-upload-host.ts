import { Component, effect, inject, input } from '@angular/core';

import { ActionDialog } from '@shared/actions/action-dialog/action-dialog';
import type { AppAction } from '@shared/actions/app-action';

import { FilesApi } from '@files/files-api';
import { FileUploadStore } from '@files/upload/file-upload.store';
import { FileUploadList } from '@files/upload/file-upload-list/file-upload-list';

const NOTHING_TO_SEND_KEY = 'FILES.UPLOAD_DIALOG.NOTHING_TO_SEND';

@Component({
  imports: [ActionDialog, FileUploadList],
  host: { class: 'contents' },
  providers: [FilesApi, FileUploadStore],
  selector: 'hf-file-upload-host',
  templateUrl: './file-upload-host.html',
})
export class FileUploadHost {
  readonly action = input.required<AppAction>();

  protected readonly store = inject(FileUploadStore);
  protected readonly nothingToSendKey = NOTHING_TO_SEND_KEY;

  constructor() {
    effect(() => {
      this.store.start(this.action());
    });
  }
}
