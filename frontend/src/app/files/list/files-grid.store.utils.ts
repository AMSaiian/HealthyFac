import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';

import { FileColumn, type FileFilter, FileSortColumn, type FilesScope } from '@files/file';

export const INITIAL_FILTER: FileFilter = {
  fileName: '',
  extensions: [],
  uploadedAt: EMPTY_RANGE,
  machine: { machineIds: [], isUntiedIncluded: false },
  ingest: { states: [], isNeverIncluded: false },
};

export const COLUMN_ORDER: readonly FileColumn[] = [
  FileColumn.FileName,
  FileColumn.Extension,
  FileColumn.SizeBytes,
  FileColumn.UploadedAt,
  FileColumn.Machine,
  FileColumn.Ingest,
];

/** The newest upload comes first, as the backend orders a request without a sort. */
export const INITIAL_GRID: GridStateInit<FileFilter, FileSortColumn, FileColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: FileSortColumn.UploadedAt, direction: SortDirection.Descending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};

/** Tells whether two scopes point to the same files, so a new object with the same values is not a new scope. */
export function isSameScope(first: FilesScope | null, second: FilesScope): boolean {
  if (first === null || first.kind !== second.kind) {
    return false;
  }
  return first.kind === 'company' || (second.kind === 'machine' && first.machineId === second.machineId);
}

export interface ScopeState {
  scope: FilesScope | null;
}
