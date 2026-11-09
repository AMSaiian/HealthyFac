import { Component, type ElementRef, input, output, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { ProgressBar } from 'primeng/progressbar';

import { ATTACHMENT_EXTENSIONS } from '@shared/files/upload-limits';

import { DESCRIPTION_MAX_LENGTH } from '@files/files-limits';
import { formatFileSize } from '@files/file.utils';
import { type UploadItem, UploadStatus } from '@files/upload/file-upload.store.utils';

export interface DescriptionChange {
  readonly key: number;
  readonly description: string;
}

@Component({
  imports: [ButtonDirective, FormsModule, InputText, ProgressBar, TranslatePipe],
  host: { class: 'flex min-h-0 flex-col gap-3' },
  selector: 'hf-file-upload-list',
  templateUrl: './file-upload-list.html',
})
export class FileUploadList {
  readonly items = input.required<readonly UploadItem[]>();

  readonly filesAdded = output<readonly File[]>();
  readonly removed = output<number>();
  readonly descriptionChanged = output<DescriptionChange>();

  protected readonly picker = viewChild.required<ElementRef<HTMLInputElement>>('picker');
  protected readonly UploadStatus = UploadStatus;
  protected readonly accept = ATTACHMENT_EXTENSIONS.map((extension) => `.${extension}`).join(',');
  protected readonly descriptionMaxLength = DESCRIPTION_MAX_LENGTH;
  protected readonly formatFileSize = formatFileSize;

  protected openPicker(): void {
    this.picker().nativeElement.click();
  }

  protected onPicked(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.filesAdded.emit(Array.from(input.files ?? []));
    input.value = '';
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.filesAdded.emit(Array.from(event.dataTransfer?.files ?? []));
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
  }

  protected canRemove(item: UploadItem): boolean {
    return item.status !== UploadStatus.Uploading;
  }

  protected canDescribe(item: UploadItem): boolean {
    return item.status === UploadStatus.Ready;
  }

  protected onDescription(item: UploadItem, description: string): void {
    this.descriptionChanged.emit({ key: item.key, description });
  }
}
