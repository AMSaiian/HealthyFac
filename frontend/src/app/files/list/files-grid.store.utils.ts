import { DEFAULT_PAGE_SIZE } from '@shared/grid/requests/grid-limits';
import { SortDirection } from '@shared/grid/requests/grid-request';
import type { GridStateInit } from '@shared/grid/with-grid-state';
import { EMPTY_RANGE } from '@shared/time/plain-date-range';

import { FileColumn, type FileFilter, FileSortColumn } from '@files/file';

export const INITIAL_FILTER: FileFilter = { fileName: '', extensions: [], uploadedAt: EMPTY_RANGE };

export const COLUMN_ORDER: readonly FileColumn[] = [
  FileColumn.FileName,
  FileColumn.Extension,
  FileColumn.SizeBytes,
  FileColumn.UploadedAt,
  FileColumn.Description,
];

/** The newest upload comes first, as the backend orders a request without a sort. */
export const INITIAL_GRID: GridStateInit<FileFilter, FileSortColumn, FileColumn> = {
  limit: DEFAULT_PAGE_SIZE,
  sort: { column: FileSortColumn.UploadedAt, direction: SortDirection.Descending },
  filter: INITIAL_FILTER,
  columns: COLUMN_ORDER,
};
