import type { DateRangeBody, SetFilterBody, TextFilterBody } from '@shared/grid/requests/grid-filters';
import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';
import type { PlainDateRange } from '@shared/time/plain-date-range';

export enum FileColumn {
  FileName = 'fileName',
  Extension = 'extension',
  SizeBytes = 'sizeBytes',
  UploadedAt = 'uploadedAt',
  Description = 'description',
}

/** The columns that the backend sorts by. */
export enum FileSortColumn {
  FileName = 'fileName',
  SizeBytes = 'sizeBytes',
  UploadedAt = 'uploadedAt',
}

export interface FileGridItem {
  readonly id: string;
  readonly fileName: string;
  readonly extension: string;
  readonly contentType: string;
  readonly sizeBytes: number;
  readonly description: string | null;
  readonly uploadedAt: string;
  readonly rowVersion: number;
}

export type FilesPage = GridPage<FileGridItem>;

export type FileSort = GridSort<FileSortColumn>;

export interface FileFilter {
  readonly fileName: string;
  readonly extensions: readonly string[];
  readonly uploadedAt: PlainDateRange;
}

export type FileGridState = GridQueryState<FileFilter, FileSortColumn>;

export interface FileFilterBody {
  readonly fileName?: TextFilterBody;
  readonly extension?: SetFilterBody<string>;
  readonly uploadedAt?: DateRangeBody;
}

export type SearchFilesBody = GridRequestBody<FileFilterBody, FileSortColumn>;

export interface UploadFileResponse {
  readonly id: string;
}

export interface UpdateFileBody {
  readonly fileName: string;
  readonly description: string | null;
  readonly rowVersion: number;
}

/** Where the files belong: to the company, or to one machine. */
export type FilesScope = { readonly kind: 'company' } | { readonly kind: 'machine'; readonly machineId: string };
