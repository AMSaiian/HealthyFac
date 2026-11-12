import type { DateRangeBody, SetFilterBody, TextFilterBody } from '@shared/grid/requests/grid-filters';
import type { GridPage, GridQueryState, GridRequestBody, GridSort } from '@shared/grid/requests/grid-request';
import type { PlainDateRange } from '@shared/time/plain-date-range';
import type { IngestState } from '@files/ingest/ingest-attempt';

export enum FileColumn {
  FileName = 'fileName',
  Extension = 'extension',
  SizeBytes = 'sizeBytes',
  UploadedAt = 'uploadedAt',
  Machine = 'machineName',
  Ingest = 'ingestState',
}

/** The columns that the backend sorts by. */
export enum FileSortColumn {
  FileName = 'fileName',
  SizeBytes = 'sizeBytes',
  UploadedAt = 'uploadedAt',
  MachineName = 'machineName',
}

export interface FileGridItem {
  readonly id: string;
  readonly fileName: string;
  readonly extension: string;
  readonly contentType: string;
  readonly sizeBytes: number;
  readonly description: string | null;
  readonly uploadedAt: string;
  readonly machineId: string | null;
  readonly machineName: string | null;
  readonly ingestState: IngestState | null;
  readonly canIngest: boolean;
  readonly rowVersion: number;
}

export type FilesPage = GridPage<FileGridItem>;

export type FileSort = GridSort<FileSortColumn>;

export interface FileFilter {
  readonly fileName: string;
  readonly extensions: readonly string[];
  readonly uploadedAt: PlainDateRange;
  readonly machine: FileMachineFilterBody;
  readonly ingest: FileIngestStateFilterBody;
}

export type FileGridState = GridQueryState<FileFilter, FileSortColumn>;

/** A backend `FileMachineFilter`: the machines, and the files that are not tied to a machine. */
export interface FileMachineFilterBody {
  readonly machineIds: readonly string[];
  readonly isUntiedIncluded: boolean;
}

/** A backend `FileIngestStateFilter`: the states, and the files that never had an attempt. */
export interface FileIngestStateFilterBody {
  readonly states: readonly IngestState[];
  readonly isNeverIncluded: boolean;
}

export interface FileFilterBody {
  readonly fileName?: TextFilterBody;
  readonly extension?: SetFilterBody<string>;
  readonly uploadedAt?: DateRangeBody;
  readonly machine?: FileMachineFilterBody;
  readonly ingestState?: FileIngestStateFilterBody;
}

export type SearchFilesBody = GridRequestBody<FileFilterBody, FileSortColumn>;

export interface UploadFileResponse {
  readonly id: string;
}

export interface TieFileBody {
  readonly machineId: string;
  readonly rowVersion: number;
}

export interface UntieFileBody {
  readonly rowVersion: number;
}

export interface UpdateFileBody {
  readonly fileName: string;
  readonly description: string | null;
  readonly rowVersion: number;
}

/** Where the files belong: to the company, or to one machine. */
export type FilesScope = { readonly kind: 'company' } | { readonly kind: 'machine'; readonly machineId: string };

/** A machine that the filter can pick. */
export interface MachineOption {
  readonly id: string;
  readonly name: string;
}
