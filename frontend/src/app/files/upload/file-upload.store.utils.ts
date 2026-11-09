import { HttpErrorResponse, HttpEventType, HttpStatusCode } from '@angular/common/http';

import type { AppAction } from '@shared/actions/app-action';
import { httpErrorMessageKey } from '@shared/http/http-error-message';

import type { FilesScope } from '@files/file';
import type { FileUploadActionPayload } from '@files/file-action-payloads';
import { checkUploadCandidate, FILE_ERROR_KEYS } from '@shared/files/upload-check';
import { ATTACHMENT_EXTENSIONS } from '@shared/files/upload-limits';

export const MAX_PARALLEL_UPLOADS = 3;
export const FILE_TYPE_NOT_ALLOWED_CODE = 'fileTypeNotAllowed';
const FILE_FIELD = 'file';
const PERCENT = 100;

export enum UploadStatus {
  Ready = 'ready',
  Refused = 'refused',
  Uploading = 'uploading',
  Done = 'done',
  Failed = 'failed',
}

export interface UploadItem {
  readonly key: number;
  readonly file: File;
  readonly description: string;
  readonly status: UploadStatus;
  readonly progress: number;
  /** The translation key of the problem, or `null`. */
  readonly errorKey: string | null;
}

export const COMPANY_SCOPE: FilesScope = { kind: 'company' };

export function scopeOf(action: AppAction | null): FilesScope {
  return (action?.payload as FileUploadActionPayload | undefined)?.scope ?? COMPANY_SCOPE;
}

/** Checks a chosen file before any request. A refused file never goes to the server. */
export function toUploadItem(file: File, key: number): UploadItem {
  const errorKey = checkUploadCandidate(file, ATTACHMENT_EXTENSIONS);
  return {
    key,
    file,
    description: '',
    status: errorKey === null ? UploadStatus.Ready : UploadStatus.Refused,
    progress: 0,
    errorKey,
  };
}

/** Maps a failed upload to the translation key that the list shows next to the file. */
export function uploadErrorKey(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return httpErrorMessageKey(0);
  }
  switch (error.status) {
    case HttpStatusCode.BadRequest:
      return hasFileTypeError(error.error) ? FILE_ERROR_KEYS.TypeNotAllowed : httpErrorMessageKey(error.status);
    case HttpStatusCode.PayloadTooLarge:
      return FILE_ERROR_KEYS.TooLarge;
    case HttpStatusCode.TooManyRequests:
      return 'FILES.ERRORS.BUSY';
    default:
      return httpErrorMessageKey(error.status);
  }
}

function hasFileTypeError(body: unknown): boolean {
  const errors = (body as { errors?: Record<string, unknown> } | null)?.errors;
  const messages = errors?.[FILE_FIELD];
  return Array.isArray(messages) && messages.includes(FILE_TYPE_NOT_ALLOWED_CODE);
}

/** Reads the upload progress in percent from an HTTP event, or `null` when the event is no progress event. */
export function progressOf(event: { type: HttpEventType; loaded?: number; total?: number }): number | null {
  if (event.type !== HttpEventType.UploadProgress) {
    return null;
  }
  return event.total === undefined || event.total === 0 ? 0 : Math.round(((event.loaded ?? 0) * PERCENT) / event.total);
}
