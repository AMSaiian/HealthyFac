import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { FileDownloader } from '@shared/files/file-downloader';
import { LookupsApi } from '@shared/lookups/lookups-api';
import { FilesLinksApi } from '@shared/files/files-links-api';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { SortDirection } from '@shared/grid/requests/grid-request';

import { type FileGridItem, FileSortColumn } from '@files/file';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';
import { IngestApi } from '@files/ingest/ingest-api';
import { IngestState } from '@files/ingest/ingest-attempt';
import { FileRowAction } from '@files/list/file-grid/file-grid.utils';

import { FilesGridStore } from './files-grid.store';

const EMPTY_PAGE = { items: [], totalCount: 0 };
const MACHINE_ID = 'encrypted-machine-id';

const FILE: FileGridItem = {
  id: 'encrypted-file-id',
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
  rowVersion: 7,
};

interface Setup {
  store: InstanceType<typeof FilesGridStore>;
  search: ReturnType<typeof vi.fn>;
  machinesSearch: ReturnType<typeof vi.fn>;
  untie: ReturnType<typeof vi.fn>;
  ingest: {
    start: ReturnType<typeof vi.fn>;
    cancel: ReturnType<typeof vi.fn>;
    listForFile: ReturnType<typeof vi.fn>;
  };
  remove: ReturnType<typeof vi.fn>;
  getLinks: ReturnType<typeof vi.fn>;
  assign: ReturnType<typeof vi.fn>;
  messages: { add: ReturnType<typeof vi.fn> };
  dispatch: ReturnType<typeof vi.fn>;
}

describe('FilesGridStore', () => {
  async function setup(
    options: {
      permissions?: readonly string[];
      links?: readonly { id: string; url: string }[];
      isConfirmed?: boolean;
      hasFile?: boolean;
    } = {},
  ): Promise<Setup> {
    const messages = { add: vi.fn() };
    const search = vi.fn(() => of(options.hasFile === true ? { items: [FILE], totalCount: 1 } : EMPTY_PAGE));
    const untie = vi.fn(() => of(undefined));
    const ingest = {
      start: vi.fn(() => of({ id: 'attempt-id' })),
      cancel: vi.fn(() => of(undefined)),
      listForFile: vi.fn(() => of({ items: [{ id: 'attempt-id', state: IngestState.Queued }] })),
    };
    const machinesSearch = vi.fn(() => of([{ id: MACHINE_ID, displayName: 'Pump' }]));
    const remove = vi.fn(() => of(undefined));
    const getLinks = vi.fn(() => of({ links: options.links ?? [] }));
    const assign = vi.fn();
    const dispatch = vi.fn(() => Promise.resolve());
    TestBed.configureTestingModule({
      providers: [
        FilesGridStore,
        { provide: FilesApi, useValue: { search, delete: remove, untie } },
        { provide: IngestApi, useValue: ingest },
        { provide: LookupsApi, useValue: { machines: machinesSearch } },
        { provide: FilesLinksApi, useValue: { getLinks } },
        { provide: FileDownloader, useValue: { start: assign } },
        { provide: MessageService, useValue: messages },
        { provide: ActionStore, useValue: { dispatch, lastResult: () => null } },
        {
          provide: ConfirmationService,
          useValue: {
            confirm: vi.fn((confirmation: { accept?: () => void }) =>
              options.isConfirmed === true ? confirmation.accept?.() : undefined,
            ),
          },
        },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
        {
          provide: AuthStore,
          useValue: { hasPermission: (permission: string) => (options.permissions ?? []).includes(permission) },
        },
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2026-01-01T00:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(FilesGridStore);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    return { store, search, machinesSearch, untie, ingest, remove, getLinks, assign, messages, dispatch };
  }

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  it('files_BeforeAScopeIsSet_SendsNoSearch', async () => {
    const { search } = await setup();

    expect(search).not.toHaveBeenCalled();
  });

  it('files_WithCompanyScope_SearchTheCompanyFilesNewestFirst', async () => {
    const { store, search } = await setup();
    store.setScope({ kind: 'company' });
    await settle();

    expect(search).toHaveBeenCalledTimes(1);
    expect(search.mock.calls[0][0]).toEqual({ kind: 'company' });
    expect(search.mock.calls[0][1].sort).toEqual({
      column: FileSortColumn.UploadedAt,
      direction: SortDirection.Descending,
    });
  });

  it('setScope_WithMachine_SearchesThatMachineAndResetsTheGrid', async () => {
    const { store, search } = await setup();
    store.setScope({ kind: 'company' });
    store.setPage(50, 10);
    await settle();

    store.setScope({ kind: 'machine', machineId: MACHINE_ID });
    await settle();

    const last = search.mock.calls.at(-1);
    expect(last?.[0]).toEqual({ kind: 'machine', machineId: MACHINE_ID });
    expect(last?.[1].offset).toBe(0);
    expect(last?.[1].limit).toBe(25);
  });

  it('setScope_WithEqualNewScopeObject_SendsNoSecondSearch', async () => {
    const { store, search } = await setup();
    store.setScope({ kind: 'machine', machineId: MACHINE_ID });
    await settle();
    const before = search.mock.calls.length;

    store.setScope({ kind: 'machine', machineId: MACHINE_ID });
    await settle();

    expect(search.mock.calls.length - before).toBe(0);
  });

  it('setFilter_WithNameAfterPaging_ResetsTheOffset', async () => {
    const { store } = await setup();
    store.setPage(50, 10);

    store.setFilter({ fileName: 'man' });

    expect(store.offset()).toBe(0);
  });

  it('permissions_ForCompanyAndMachineScope_UseTheirOwnPermissions', async () => {
    const { store } = await setup({ permissions: [Permission.CompanyFilesWrite, Permission.FilesDelete] });
    store.setScope({ kind: 'company' });

    expect(store.canUpload()).toBe(true);
    expect(store.canDelete()).toBe(false);

    store.setScope({ kind: 'machine', machineId: MACHINE_ID });

    expect(store.canUpload()).toBe(false);
    expect(store.canDelete()).toBe(true);
  });

  it('rowAccess_ForCompanyAndMachineScope_GrantsIngestAndTieOnTheCompanyPageOnly', async () => {
    const { store } = await setup({
      permissions: [Permission.IngestWrite, Permission.IngestRead, Permission.CompanyFilesWrite],
    });
    store.setScope({ kind: 'company' });

    expect(store.rowAccess()).toEqual({ canIngestWrite: true, canIngestRead: true, canTie: true });

    store.setScope({ kind: 'machine', machineId: MACHINE_ID });

    expect(store.rowAccess()).toEqual({ canIngestWrite: false, canIngestRead: false, canTie: false });
  });

  it('machineOptions_WithoutMachinesRead_LoadsNothingAndListsNoOptions', async () => {
    const { store, machinesSearch } = await setup();
    store.setScope({ kind: 'company' });
    await settle();

    expect(machinesSearch).not.toHaveBeenCalled();
    expect(store.machineOptions()).toEqual([]);
  });

  it('machineOptions_ForCompanyScope_LoadsTheMachinesOfTheCompanyOnly', async () => {
    const { store, machinesSearch } = await setup({ permissions: [Permission.MachinesRead] });
    store.setScope({ kind: 'machine', machineId: MACHINE_ID });
    await settle();

    expect(machinesSearch).not.toHaveBeenCalled();

    store.setScope({ kind: 'company' });
    await settle();

    expect(machinesSearch).toHaveBeenCalledTimes(1);
    expect(store.machineOptions()).toEqual([{ id: MACHINE_ID, name: 'Pump' }]);
  });

  it('requestRowAction_Ingest_ShowsTheRowQueuedAtOnceWithoutAnotherSearch', async () => {
    const { store, search, ingest } = await setup({ hasFile: true });
    store.setScope({ kind: 'company' });
    await settle();
    const searches = search.mock.calls.length;

    store.requestRowAction({ action: FileRowAction.Ingest, file: FILE });
    await settle();

    expect(ingest.start).toHaveBeenCalledWith(FILE.id, expect.anything());
    expect(store.files.value()?.items[0]).toMatchObject({ ingestState: IngestState.Queued, canIngest: false });
    expect(search.mock.calls.length).toBe(searches);
  });

  it('requestRowAction_IngestWithConflict_KeepsTheRowShowsTheReasonAndReloads', async () => {
    const { store, search, ingest, messages } = await setup({ hasFile: true });
    ingest.start.mockReturnValue(
      throwError(
        () => new HttpErrorResponse({ status: HttpStatusCode.Conflict, error: { detail: 'ingestCompleted' } }),
      ),
    );
    store.setScope({ kind: 'company' });
    await settle();
    const searches = search.mock.calls.length;

    store.requestRowAction({ action: FileRowAction.Ingest, file: FILE });
    await settle();

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'INGEST.CONFLICTS.INGEST_COMPLETED' }));
    expect(search.mock.calls.length).toBe(searches + 1);
  });

  it('requestRowAction_CancelIngestWhenConfirmed_CancelsTheActiveAttemptAndShowsInterrupted', async () => {
    const { store, ingest } = await setup({ hasFile: true, isConfirmed: true });
    store.setScope({ kind: 'company' });
    await settle();

    store.requestRowAction({ action: FileRowAction.CancelIngest, file: { ...FILE, ingestState: IngestState.Queued } });
    await settle();

    expect(ingest.listForFile).toHaveBeenCalledWith(FILE.id, expect.objectContaining({ context: expect.anything() }));
    expect(ingest.cancel).toHaveBeenCalledWith('attempt-id', expect.anything());
    expect(store.files.value()?.items[0]).toMatchObject({ ingestState: IngestState.Interrupted, canIngest: true });
  });

  it('requestRowAction_CancelIngestWhenNotConfirmed_CallsNothing', async () => {
    const { store, ingest } = await setup({ hasFile: true });
    store.setScope({ kind: 'company' });
    await settle();

    store.requestRowAction({ action: FileRowAction.CancelIngest, file: FILE });
    await settle();

    expect(ingest.listForFile).not.toHaveBeenCalled();
    expect(ingest.cancel).not.toHaveBeenCalled();
  });

  it('requestRowAction_UntieWhenConfirmed_SendsTheRowVersionAndReloads', async () => {
    const { store, untie, search } = await setup({ hasFile: true, isConfirmed: true });
    store.setScope({ kind: 'company' });
    await settle();
    const searches = search.mock.calls.length;

    store.requestRowAction({ action: FileRowAction.Untie, file: { ...FILE, machineId: MACHINE_ID } });
    await settle();

    expect(untie).toHaveBeenCalledWith(FILE.id, { rowVersion: FILE.rowVersion }, expect.anything());
    expect(search.mock.calls.length).toBe(searches + 1);
  });

  it('requestRowAction_Tie_DispatchesTheTieActionWithTheRowVersion', async () => {
    const { store, dispatch } = await setup();

    store.requestRowAction({ action: FileRowAction.Tie, file: FILE });

    expect(dispatch).toHaveBeenCalledWith({
      type: FileActionType.Tie,
      payload: { item: { id: FILE.id, fileName: FILE.fileName, rowVersion: FILE.rowVersion } },
    });
  });

  it('download_WithLink_AsksOneLinkAndNavigatesToIt', async () => {
    const { store, getLinks, assign } = await setup({ links: [{ id: FILE.id, url: 'https://api/blob?sig=x' }] });

    store.download(FILE);
    await settle();

    expect(getLinks).toHaveBeenCalledWith([FILE.id]);
    expect(assign).toHaveBeenCalledWith('https://api/blob?sig=x');
  });

  it('download_WithoutLink_ShowsTheNotFoundToastAndReloadsTheGrid', async () => {
    const { store, search, assign, messages } = await setup({ links: [] });
    store.setScope({ kind: 'company' });
    await settle();
    const searches = search.mock.calls.length;

    store.download(FILE);
    await settle();

    expect(assign).not.toHaveBeenCalled();
    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'FILES.ERRORS.NOT_FOUND' }));
    expect(search.mock.calls.length).toBe(searches + 1);
  });

  it('requestUpload_Always_DispatchesTheUploadActionWithTheScope', async () => {
    const { store, dispatch } = await setup();
    store.setScope({ kind: 'company' });

    store.requestUpload();

    expect(dispatch).toHaveBeenCalledWith({ type: FileActionType.Upload, payload: { scope: { kind: 'company' } } });
  });

  it('requestDelete_WhenConfirmed_DeletesInTheScopeAndReloads', async () => {
    const { store, remove, search } = await setup({ isConfirmed: true });
    store.setScope({ kind: 'machine', machineId: MACHINE_ID });
    await settle();
    const searches = search.mock.calls.length;

    store.requestDelete(FILE);
    await settle();

    expect(remove).toHaveBeenCalledWith({ kind: 'machine', machineId: MACHINE_ID }, FILE.id, expect.anything());
    expect(search.mock.calls.length).toBe(searches + 1);
  });

  it('requestDelete_WhenTheFileIsGone_ShowsNotFoundAndReloads', async () => {
    const { store, remove, search, messages } = await setup({ isConfirmed: true });
    remove.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));
    store.setScope({ kind: 'machine', machineId: MACHINE_ID });
    await settle();
    const searches = search.mock.calls.length;

    store.requestDelete(FILE);
    await settle();

    expect(messages.add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'FILES.ERRORS.NOT_FOUND' }));
    expect(search.mock.calls.length).toBe(searches + 1);
  });
});
