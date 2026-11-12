import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';

import { FileTypeTag } from '@shared/files/file-type-tag/file-type-tag';
import { delayedLoading } from '@shared/loading/delayed-loading';

import type { FileEntry } from '@work-orders/drawer/work-order-drawer-store/work-order-drawer.store.utils';

@Component({
  imports: [ButtonDirective, FileTypeTag, Skeleton, Tooltip, TranslatePipe],
  host: { class: 'flex flex-col gap-2' },
  selector: 'hf-work-order-files',
  templateUrl: './work-order-files.html',
})
export class WorkOrderFiles {
  readonly entries = input.required<readonly FileEntry[]>();
  readonly isLoading = input(false);
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  readonly isEditable = input(true);
  readonly isFull = input(false);

  readonly removed = output<string>();
  readonly uploadChosen = output<File>();
  readonly pickRequested = output<void>();

  protected readonly isEmpty = computed(() => this.entries().length === 0);

  protected onFileChosen(event: Event): void {
    const input = event.target as HTMLInputElement;
    const [file] = Array.from(input.files ?? []);
    input.value = '';
    if (file !== undefined) {
      this.uploadChosen.emit(file);
    }
  }
}
