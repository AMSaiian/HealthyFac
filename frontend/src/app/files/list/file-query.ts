import { dateRangeFilter, setFilter, textFilter } from '@shared/grid/requests/grid-filters';

import type {
  FileFilter,
  FileFilterBody,
  FileGridState,
  FileIngestStateFilterBody,
  FileMachineFilterBody,
  SearchFilesBody,
} from '@files/file';

function machineFilter(machine: FileFilter['machine']): FileMachineFilterBody | undefined {
  return machine.machineIds.length === 0 && !machine.isUntiedIncluded ? undefined : machine;
}

function ingestFilter(ingest: FileFilter['ingest']): FileIngestStateFilterBody | undefined {
  return ingest.states.length === 0 && !ingest.isNeverIncluded ? undefined : ingest;
}

export function toSearchBody(state: FileGridState, timeZoneId: string): SearchFilesBody {
  const { filter } = state;
  return {
    offset: state.offset,
    limit: state.limit,
    ...(state.sort === null ? {} : { sort: state.sort }),
    filter: {
      fileName: textFilter(filter.fileName),
      extension: setFilter(filter.extensions),
      uploadedAt: dateRangeFilter(filter.uploadedAt, timeZoneId),
      machine: machineFilter(filter.machine),
      ingestState: ingestFilter(filter.ingest),
    } satisfies FileFilterBody,
  };
}
