import { Component, computed, DestroyRef, inject, input, model, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import type { FormValueControl } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { Editor, type EditorInitEvent } from 'primeng/editor';
import { ProgressSpinner } from 'primeng/progressspinner';
import { Tooltip } from 'primeng/tooltip';
import Quill from 'quill';
import { finalize } from 'rxjs';

import { checkUploadCandidate } from '@shared/files/upload-check';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EMBEDDED_MAX_BYTES, IMAGE_EXTENSIONS, IMAGE_MIME_TYPES } from '@shared/files/upload-limits';
import {
  FILE_ID_ATTRIBUTE,
  FILE_IMAGE_BLOT_NAME,
  keepFileImagesOnly,
  PENDING_CLASS,
  registerFileImageBlot,
} from '@shared/rich-text/file-image/file-image-blot';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { imageAltOf, MISSING_IMAGE_CLASS } from '@shared/rich-text/file-image/rich-text-images';
import { normalizeRichTextHtml } from '@shared/rich-text/rich-text-html';
import { visibleTextLength } from '@shared/rich-text/rich-text-length';

interface QuillUploader {
  upload: (range: { index: number } | null, files: FileList | readonly File[]) => void;
}

const EMPTY_EDITOR_HTML = '<p><br></p>';
const IMAGE_SELECTOR = `img[${FILE_ID_ATTRIBUTE}]`;

registerFileImageBlot();

@Component({
  // eslint-disable-next-line @typescript-eslint/no-deprecated -- PrimeNG Editor stays until a free replacement is chosen (user decision 2026-11-04).
  imports: [Editor, FormsModule, ProgressSpinner, Tooltip, TranslatePipe],
  host: { class: 'flex flex-col gap-1' },
  selector: 'hf-rich-text-editor',
  templateUrl: './rich-text-editor.html',
})
export class RichTextEditor implements FormValueControl<string | null> {
  readonly value = model<string | null>(null);
  readonly characterLimit = input<number | undefined>(undefined);
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly invalid = input(false);
  readonly touched = input(false);
  readonly placeholder = input('');
  readonly touch = output<void>();

  protected readonly isInvalidShown = computed(() => this.invalid() && this.touched());
  protected readonly length = computed(() => visibleTextLength(this.value()));
  protected readonly isTooLong = computed(() => this.length() > (this.characterLimit() ?? Infinity));
  protected readonly pendingUploads = signal(0);
  /** The editor is read-only for the whole upload; the spinner shows only after the delay of the shared spinner. */
  protected readonly showUploadSpinner = delayedLoading(computed(() => this.pendingUploads() > 0));

  /**
   * The Quill modules. The clipboard drops every image that is no file image. The uploader sends a chosen, pasted, or
   * dropped image file to the Document service, instead of its default that embeds the file as a data URL. Its
   * `mimetypes` only fill the `accept` of the file picker of the image button: `onInit` sends every file to
   * `_upload`, so one check decides.
   */
  protected readonly modules = {
    clipboard: { matchers: [['img', keepFileImagesOnly]] },
    uploader: {
      mimetypes: IMAGE_MIME_TYPES,
      handler: (range: { index: number } | null, files: File[]): void => this._upload(range, files),
    },
  };

  private readonly _links = inject(RichTextImageLinks);
  private readonly _messages = inject(MessageService);
  private readonly _translate = inject(TranslateService);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _handled = new WeakSet<Element>();
  private readonly _claimed = new WeakSet<Element>();
  private _quill: Quill | null = null;

  protected onEditorChange(html: string | null): void {
    this.value.set(html === null || html === '' || html === EMPTY_EDITOR_HTML ? null : normalizeRichTextHtml(html));
  }

  protected onInit(event: EditorInitEvent): void {
    this._quill = event.editor as Quill;
    const uploader = this._quill.getModule('uploader') as QuillUploader;
    uploader.upload = (range, files) => this._upload(range, Array.from(files));
    const observer = new MutationObserver(() => this._applyLinks());
    observer.observe(this._quill.root, { childList: true, subtree: true });
    this._destroyRef.onDestroy(() => observer.disconnect());
    this._applyLinks();
  }

  /**
   * Gives each file image its `src` as a DOM-only attribute. The blot ignores `src` in `value` and `html`, so the form
   * value stays free of the short-lived link. An image without a link shows its alt text.
   */
  private _applyLinks(): void {
    const root = this._quill?.root;
    if (root === undefined) {
      return;
    }
    const images = Array.from(root.querySelectorAll(IMAGE_SELECTOR)).filter(
      (image) => !this._handled.has(image) && image.getAttribute(FILE_ID_ATTRIBUTE) !== '',
    );
    if (images.length === 0) {
      return;
    }
    images.forEach((image) => this._handled.add(image));
    const ids = [...new Set(images.map((image) => image.getAttribute(FILE_ID_ATTRIBUTE) ?? ''))];
    this._links
      .resolve(ids)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe((links) => {
        for (const image of images) {
          const url = links.get(image.getAttribute(FILE_ID_ATTRIBUTE) ?? '');
          if (url === undefined) {
            image.classList.add(MISSING_IMAGE_CLASS);
            image.setAttribute('title', this._translate.instant('RICH_TEXT.IMAGE_MISSING'));
          } else {
            image.setAttribute('src', url);
          }
        }
      });
  }

  private _upload(range: { index: number } | null, files: readonly File[]): void {
    const quill = this._quill;
    if (quill === null) {
      return;
    }
    let index = Math.min(range?.index ?? quill.getLength(), quill.getLength() - 1);
    for (const file of files) {
      if (checkUploadCandidate(file, IMAGE_EXTENSIONS, EMBEDDED_MAX_BYTES) !== null) {
        this._showError('RICH_TEXT.IMAGE_NOT_ALLOWED');
        continue;
      }
      this._insertPlaceholderAndUpload(quill, index, file);
      index += 1;
    }
  }

  private _insertPlaceholderAndUpload(quill: Quill, index: number, file: File): void {
    quill.insertEmbed(index, FILE_IMAGE_BLOT_NAME, { fileId: '', alt: imageAltOf(file.name) }, 'user');
    const placeholder = Array.from(quill.root.querySelectorAll<HTMLElement>(`.${PENDING_CLASS}`)).find(
      (candidate) => !this._claimed.has(candidate),
    );
    if (placeholder !== undefined) {
      this._claimed.add(placeholder);
    }
    this.pendingUploads.update((count) => count + 1);
    this._links
      .upload(file)
      .pipe(
        takeUntilDestroyed(this._destroyRef),
        finalize(() => this.pendingUploads.update((count) => count - 1)),
      )
      .subscribe({
        next: (fileId) => this._replacePlaceholder(quill, placeholder, { fileId, alt: imageAltOf(file.name) }),
        error: () => {
          this._replacePlaceholder(quill, placeholder, null);
          this._showError('RICH_TEXT.IMAGE_UPLOAD_FAILED');
        },
      });
  }

  private _replacePlaceholder(
    quill: Quill,
    placeholder: HTMLElement | undefined,
    image: { fileId: string; alt: string } | null,
  ): void {
    const blot = placeholder === undefined ? null : Quill.find(placeholder);
    if (blot === null || blot instanceof Quill) {
      return;
    }
    const index = quill.getIndex(blot);
    const wasEnabled = quill.isEnabled();
    quill.enable();
    quill.deleteText(index, 1, 'user');
    if (image !== null) {
      quill.insertEmbed(index, FILE_IMAGE_BLOT_NAME, image, 'user');
    }
    if (!wasEnabled) {
      quill.disable();
    }
  }

  private _showError(detailKey: string): void {
    this._messages.add({
      severity: 'error',
      summary: this._translate.instant('TOASTS.ERROR'),
      detail: this._translate.instant(detailKey),
    });
  }
}
