import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { FileDownloader } from '@shared/files/file-downloader';
import { FilesLinksApi } from '@shared/files/files-links-api';
import { AuthStore } from '@shared/security/auth.store';
import { Permission } from '@shared/security/permissions';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { SortDirection } from '@shared/grid/requests/grid-request';

import { type FileGridItem, FileSortColumn } from '@files/file';
import { FileActionType } from '@files/file-actions';
import { FilesApi } from '@files/files-api';

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
  rowVersion: 7,
};

interface Setup {
  store: InstanceType<typeof FilesGridStore>;
  search: ReturnType<typeof vi.fn>;
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
    } = {},
  ): Promise<Setup> {
    const messages = { add: vi.fn() };
    const search = vi.fn(() => of(EMPTY_PAGE));
    const remove = vi.fn(() => of(undefined));
    const getLinks = vi.fn(() => of({ links: options.links ?? [] }));
    const assign = vi.fn();
    const dispatch = vi.fn(() => Promise.resolve());
    TestBed.configureTestingModule({
      providers: [
        FilesGridStore,
        { provide: FilesApi, useValue: { search, delete: remove } },
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
    return { store, search, remove, getLinks, assign, messages, dispatch };
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
