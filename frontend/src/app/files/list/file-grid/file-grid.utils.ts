import { asList, asRange, asText } from '@shared/grid/table/table-filters';
import { isRangeEmpty } from '@shared/time/plain-date-range';

import { FileColumn, type FileFilter, FileSortColumn } from '@files/file';
import { ATTACHMENT_EXTENSIONS } from '@shared/files/upload-limits';

export const COLUMN_LABEL_KEYS: Readonly<Record<FileColumn, string>> = {
  [FileColumn.FileName]: 'FILES.COLUMNS.FILE_NAME',
  [FileColumn.Extension]: 'FILES.COLUMNS.TYPE',
  [FileColumn.SizeBytes]: 'FILES.COLUMNS.SIZE',
  [FileColumn.UploadedAt]: 'FILES.COLUMNS.UPLOADED_AT',
  [FileColumn.Description]: 'FILES.COLUMNS.DESCRIPTION',
};

export const MENU_DOWNLOAD_KEY = 'FILES.ACTIONS_MENU.DOWNLOAD';
export const MENU_EDIT_KEY = 'FILES.ACTIONS_MENU.EDIT';
export const MENU_DELETE_KEY = 'FILES.ACTIONS_MENU.DELETE';

export const SORTABLE_COLUMNS: readonly string[] = Object.values(FileSortColumn);

/** The extension options of the type filter, in upper case for the user. `jpeg` stays out: the server stores `jpg`. */
export const EXTENSION_OPTIONS: { value: string; label: string }[] = ATTACHMENT_EXTENSIONS.filter(
  (extension) => extension !== 'jpeg',
).map((extension) => ({
  value: extension,
  label: extension.toUpperCase(),
}));

/** Maps the pending value of one column filter of the table to the store filter. A missing value means "empty". */
export const FILTER_READERS: Readonly<Record<FileColumn, (value: unknown) => Partial<FileFilter>>> = {
  [FileColumn.FileName]: (value) => ({ fileName: asText(value) }),
  [FileColumn.Extension]: (value) => ({ extensions: asList<string>(value) }),
  [FileColumn.SizeBytes]: () => ({}),
  [FileColumn.UploadedAt]: (value) => ({ uploadedAt: asRange(value) }),
  [FileColumn.Description]: () => ({}),
};

/** Reads the applied store value of one column as a table filter value. `null` means "no filter". */
export const FILTER_WRITERS: Readonly<Record<FileColumn, (filter: FileFilter) => unknown>> = {
  [FileColumn.FileName]: (filter) => filter.fileName || null,
  [FileColumn.Extension]: (filter) => (filter.extensions.length === 0 ? null : filter.extensions),
  [FileColumn.SizeBytes]: () => null,
  [FileColumn.UploadedAt]: (filter) => (isRangeEmpty(filter.uploadedAt) ? null : filter.uploadedAt),
  [FileColumn.Description]: () => null,
};

export const FILTERABLE_COLUMNS: readonly FileColumn[] = [
  FileColumn.FileName,
  FileColumn.Extension,
  FileColumn.UploadedAt,
];
