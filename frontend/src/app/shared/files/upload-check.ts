import { ATTACHMENT_MAX_BYTES } from '@shared/files/upload-limits';

export const FILE_ERROR_KEYS = {
  TypeNotAllowed: 'FILES.ERRORS.TYPE_NOT_ALLOWED',
  TooLarge: 'FILES.ERRORS.TOO_LARGE',
  Empty: 'FILES.ERRORS.EMPTY',
} as const;

export type FileErrorKey = (typeof FILE_ERROR_KEYS)[keyof typeof FILE_ERROR_KEYS];

/** Returns the extension of a file name in lower case, or `null` when the name has none. */
export function fileExtensionOf(name: string): string | null {
  const segment = name.slice(Math.max(name.lastIndexOf('/'), name.lastIndexOf('\\')) + 1);
  const dotIndex = segment.lastIndexOf('.');
  return dotIndex < 0 || dotIndex === segment.length - 1 ? null : segment.slice(dotIndex + 1).toLowerCase();
}

/**
 * Checks a chosen file before any request. The server proves the real type again, so this check only saves a round
 * trip. It returns the translation key of the first problem, or `null` when the file may go.
 */
export function checkUploadCandidate(
  file: Pick<File, 'name' | 'size'>,
  allowedExtensions: readonly string[],
  maxBytes: number = ATTACHMENT_MAX_BYTES,
): FileErrorKey | null {
  const extension = fileExtensionOf(file.name);
  if (extension === null || !allowedExtensions.includes(extension)) {
    return FILE_ERROR_KEYS.TypeNotAllowed;
  }
  if (file.size === 0) {
    return FILE_ERROR_KEYS.Empty;
  }
  return file.size > maxBytes ? FILE_ERROR_KEYS.TooLarge : null;
}

/** Returns the file name without its folder and its last extension. */
export function fileBaseNameOf(name: string): string {
  const segment = name.slice(Math.max(name.lastIndexOf('/'), name.lastIndexOf('\\')) + 1);
  const dotIndex = segment.lastIndexOf('.');
  return dotIndex <= 0 ? segment : segment.slice(0, dotIndex);
}
