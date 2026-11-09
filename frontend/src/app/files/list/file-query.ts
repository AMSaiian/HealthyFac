import { dateRangeFilter, setFilter, textFilter } from '@shared/grid/requests/grid-filters';

import type { FileGridState, SearchFilesBody } from '@files/file';

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
    },
  };
}
