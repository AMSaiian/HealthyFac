import type { FileGridItem } from '@files/file';
import { IngestState } from '@files/ingest/ingest-attempt';

import {
  FileRowAction,
  NEVER_OPTION,
  NO_ROW_ACCESS,
  NOT_TIED_OPTION,
  toIngestFilter,
  toIngestValues,
  toMachineFilter,
  toMachineValues,
  visibleRowActions,
} from './file-grid.utils';

const FILE: FileGridItem = {
  id: 'f1',
  fileName: 'manual',
  extension: 'pdf',
  contentType: 'application/pdf',
  sizeBytes: 10,
  description: null,
  uploadedAt: '2026-03-01T08:00:00Z',
  machineId: null,
  machineName: null,
  ingestState: null,
  canIngest: true,
  rowVersion: 1,
};

const ALL = { canIngestWrite: true, canIngestRead: true, canTie: true };

describe('visibleRowActions', () => {
  it('visibleRowActions_ForUntiedFileWithoutAttempt_OffersIngestAndTie', () => {
    expect(visibleRowActions(FILE, ALL)).toEqual([FileRowAction.Ingest, FileRowAction.Tie]);
  });

  it('visibleRowActions_ForQueuedTiedFile_OffersCancelHistoryAndUntie', () => {
    const file = { ...FILE, canIngest: false, ingestState: IngestState.Queued, machineId: 'm1' };

    expect(visibleRowActions(file, ALL)).toEqual([
      FileRowAction.CancelIngest,
      FileRowAction.History,
      FileRowAction.Untie,
    ]);
  });

  it('visibleRowActions_ForCompletedFile_OffersNoIngestAndNoCancel', () => {
    const file = { ...FILE, canIngest: false, ingestState: IngestState.Completed };

    expect(visibleRowActions(file, ALL)).toEqual([FileRowAction.History, FileRowAction.Tie]);
  });

  it('visibleRowActions_WithoutPermissions_OffersNothing', () => {
    const file = { ...FILE, ingestState: IngestState.InProcess };

    expect(visibleRowActions(file, NO_ROW_ACCESS)).toEqual([]);
  });

  it('visibleRowActions_WithReadOnlyIngest_OffersHistoryOnly', () => {
    const file = { ...FILE, ingestState: IngestState.Failed };

    expect(visibleRowActions(file, { ...NO_ROW_ACCESS, canIngestRead: true })).toEqual([FileRowAction.History]);
  });
});

describe('filter value mapping', () => {
  it('toMachineFilter_WithNotTiedAndIds_SplitsThePseudoValue', () => {
    expect(toMachineFilter([NOT_TIED_OPTION, 'm1'])).toEqual({ machineIds: ['m1'], isUntiedIncluded: true });
    expect(toMachineValues({ machineIds: ['m1'], isUntiedIncluded: true })).toEqual([NOT_TIED_OPTION, 'm1']);
  });

  it('toMachineValues_WithEmptyFilter_ReturnsNull', () => {
    expect(toMachineValues({ machineIds: [], isUntiedIncluded: false })).toBeNull();
  });

  it('toIngestFilter_WithNeverAndState_SplitsThePseudoValue', () => {
    expect(toIngestFilter([NEVER_OPTION, IngestState.Failed])).toEqual({
      states: [IngestState.Failed],
      isNeverIncluded: true,
    });
    expect(toIngestValues({ states: [IngestState.Failed], isNeverIncluded: true })).toEqual([
      NEVER_OPTION,
      IngestState.Failed,
    ]);
  });
});
