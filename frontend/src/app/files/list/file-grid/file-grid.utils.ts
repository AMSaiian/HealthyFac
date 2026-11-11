import { asList, asRange, asText } from '@shared/grid/table/table-filters';
import { isRangeEmpty } from '@shared/time/plain-date-range';

import { FileColumn, type FileFilter, type FileGridItem, FileSortColumn } from '@files/file';
import { IngestState } from '@files/ingest/ingest-attempt';
import { ATTACHMENT_EXTENSIONS } from '@shared/files/upload-limits';

export const COLUMN_LABEL_KEYS: Readonly<Record<FileColumn, string>> = {
  [FileColumn.FileName]: 'FILES.COLUMNS.FILE_NAME',
  [FileColumn.Extension]: 'FILES.COLUMNS.TYPE',
  [FileColumn.SizeBytes]: 'FILES.COLUMNS.SIZE',
  [FileColumn.UploadedAt]: 'FILES.COLUMNS.UPLOADED_AT',
  [FileColumn.Description]: 'FILES.COLUMNS.DESCRIPTION',
  [FileColumn.Machine]: 'FILES.COLUMNS.MACHINE',
  [FileColumn.Ingest]: 'FILES.COLUMNS.INGEST',
};

/** The two columns that only the company page shows. */
export const INGEST_COLUMNS: readonly FileColumn[] = [FileColumn.Machine, FileColumn.Ingest];

export const MENU_DOWNLOAD_KEY = 'FILES.ACTIONS_MENU.DOWNLOAD';
export const MENU_EDIT_KEY = 'FILES.ACTIONS_MENU.EDIT';
export const MENU_DELETE_KEY = 'FILES.ACTIONS_MENU.DELETE';
export const MENU_INGEST_KEY = 'FILES.ACTIONS_MENU.INGEST';
export const MENU_CANCEL_INGEST_KEY = 'FILES.ACTIONS_MENU.CANCEL_INGEST';
export const MENU_HISTORY_KEY = 'FILES.ACTIONS_MENU.HISTORY';
export const MENU_TIE_KEY = 'FILES.ACTIONS_MENU.TIE';
export const MENU_UNTIE_KEY = 'FILES.ACTIONS_MENU.UNTIE';
export const FILTER_NOT_TIED_KEY = 'FILES.FILTERS.NOT_TIED';

/** The option values of the two pseudo entries in the filter lists. The IDs of machines never look like them. */
export const NOT_TIED_OPTION = 'notTied';
export const NEVER_OPTION = 'never';

export const SORTABLE_COLUMNS: readonly string[] = Object.values(FileSortColumn);

/** The extension options of the type filter, in upper case for the user. `jpeg` stays out: the server stores `jpg`. */
export const EXTENSION_OPTIONS: { value: string; label: string }[] = ATTACHMENT_EXTENSIONS.filter(
  (extension) => extension !== 'jpeg',
).map((extension) => ({
  value: extension,
  label: extension.toUpperCase(),
}));

export const INGEST_STATE_LABEL_KEYS: Readonly<Record<IngestState, string>> = {
  [IngestState.Queued]: 'INGEST.STATES.QUEUED',
  [IngestState.InProcess]: 'INGEST.STATES.IN_PROCESS',
  [IngestState.Completed]: 'INGEST.STATES.COMPLETED',
  [IngestState.Failed]: 'INGEST.STATES.FAILED',
  [IngestState.Interrupted]: 'INGEST.STATES.INTERRUPTED',
};

export const INGEST_NEVER_LABEL_KEY = 'INGEST.STATES.NEVER';

export type IngestTagSeverity = 'info' | 'success' | 'danger' | 'warn' | 'secondary';

const INGEST_SEVERITIES: Readonly<Record<IngestState, IngestTagSeverity>> = {
  [IngestState.Queued]: 'info',
  [IngestState.InProcess]: 'info',
  [IngestState.Completed]: 'success',
  [IngestState.Failed]: 'danger',
  [IngestState.Interrupted]: 'warn',
};

/** The tag severity of an ingest state. A file without an attempt ("never") is `secondary`. */
export function ingestSeverity(state: IngestState | null): IngestTagSeverity {
  return state === null ? 'secondary' : INGEST_SEVERITIES[state];
}

export function ingestLabelKey(state: IngestState | null): string {
  return state === null ? INGEST_NEVER_LABEL_KEY : INGEST_STATE_LABEL_KEYS[state];
}

/** Maps the machine filter to the values of the multi select. The pseudo value stands for the untied files. */
export function toMachineValues(machine: FileFilter['machine']): string[] | null {
  const values = machine.isUntiedIncluded ? [NOT_TIED_OPTION, ...machine.machineIds] : [...machine.machineIds];
  return values.length === 0 ? null : values;
}

export function toMachineFilter(value: unknown): FileFilter['machine'] {
  const values = asList<string>(value);
  return {
    machineIds: values.filter((candidate) => candidate !== NOT_TIED_OPTION),
    isUntiedIncluded: values.includes(NOT_TIED_OPTION),
  };
}

/** Maps the ingest filter to the values of the multi select. The pseudo value stands for the files without attempt. */
export function toIngestValues(ingest: FileFilter['ingest']): string[] | null {
  const values: string[] = ingest.isNeverIncluded ? [NEVER_OPTION, ...ingest.states] : [...ingest.states];
  return values.length === 0 ? null : values;
}

export function toIngestFilter(value: unknown): FileFilter['ingest'] {
  const values = asList<string>(value);
  return {
    states: values.filter((candidate): candidate is IngestState => candidate !== NEVER_OPTION),
    isNeverIncluded: values.includes(NEVER_OPTION),
  };
}

/** Maps the pending value of one column filter of the table to the store filter. A missing value means "empty". */
export const FILTER_READERS: Readonly<Record<FileColumn, (value: unknown) => Partial<FileFilter>>> = {
  [FileColumn.FileName]: (value) => ({ fileName: asText(value) }),
  [FileColumn.Extension]: (value) => ({ extensions: asList<string>(value) }),
  [FileColumn.SizeBytes]: () => ({}),
  [FileColumn.UploadedAt]: (value) => ({ uploadedAt: asRange(value) }),
  [FileColumn.Description]: () => ({}),
  [FileColumn.Machine]: (value) => ({ machine: toMachineFilter(value) }),
  [FileColumn.Ingest]: (value) => ({ ingest: toIngestFilter(value) }),
};

/** Reads the applied store value of one column as a table filter value. `null` means "no filter". */
export const FILTER_WRITERS: Readonly<Record<FileColumn, (filter: FileFilter) => unknown>> = {
  [FileColumn.FileName]: (filter) => filter.fileName || null,
  [FileColumn.Extension]: (filter) => (filter.extensions.length === 0 ? null : filter.extensions),
  [FileColumn.SizeBytes]: () => null,
  [FileColumn.UploadedAt]: (filter) => (isRangeEmpty(filter.uploadedAt) ? null : filter.uploadedAt),
  [FileColumn.Description]: () => null,
  [FileColumn.Machine]: (filter) => toMachineValues(filter.machine),
  [FileColumn.Ingest]: (filter) => toIngestValues(filter.ingest),
};

export const FILTERABLE_COLUMNS: readonly FileColumn[] = [
  FileColumn.FileName,
  FileColumn.Extension,
  FileColumn.UploadedAt,
  FileColumn.Machine,
  FileColumn.Ingest,
];

/** The row actions that the ingest feature adds to the menu of a company file. */
export enum FileRowAction {
  Ingest = 'ingest',
  CancelIngest = 'cancelIngest',
  History = 'history',
  Tie = 'tie',
  Untie = 'untie',
}

/** What the signed-in user may do with ingest and ties. All `false` on the machine files page. */
export interface FileRowAccess {
  readonly canIngestWrite: boolean;
  readonly canIngestRead: boolean;
  readonly canTie: boolean;
}

export const NO_ROW_ACCESS: FileRowAccess = { canIngestWrite: false, canIngestRead: false, canTie: false };

export const ROW_ACTION_KEYS: Readonly<Record<FileRowAction, string>> = {
  [FileRowAction.Ingest]: MENU_INGEST_KEY,
  [FileRowAction.CancelIngest]: MENU_CANCEL_INGEST_KEY,
  [FileRowAction.History]: MENU_HISTORY_KEY,
  [FileRowAction.Tie]: MENU_TIE_KEY,
  [FileRowAction.Untie]: MENU_UNTIE_KEY,
};

/** Lists the ingest and tie actions that the menu of one row shows: each one needs its state and its permission. */
export function visibleRowActions(file: FileGridItem, access: FileRowAccess): FileRowAction[] {
  const isActive = file.ingestState === IngestState.Queued || file.ingestState === IngestState.InProcess;
  return [
    ...(file.canIngest && access.canIngestWrite ? [FileRowAction.Ingest] : []),
    ...(isActive && access.canIngestWrite ? [FileRowAction.CancelIngest] : []),
    ...(file.ingestState !== null && access.canIngestRead ? [FileRowAction.History] : []),
    ...(file.machineId === null && access.canTie ? [FileRowAction.Tie] : []),
    ...(file.machineId !== null && access.canTie ? [FileRowAction.Untie] : []),
  ];
}

export const ROW_ACTION_ICONS: Readonly<Record<FileRowAction, string>> = {
  [FileRowAction.Ingest]: 'pi pi-play',
  [FileRowAction.CancelIngest]: 'pi pi-times-circle',
  [FileRowAction.History]: 'pi pi-history',
  [FileRowAction.Tie]: 'pi pi-link',
  [FileRowAction.Untie]: 'pi pi-ban',
};
