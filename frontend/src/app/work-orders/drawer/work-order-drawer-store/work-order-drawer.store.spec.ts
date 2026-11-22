import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { LookupsApi } from '@shared/lookups/lookups-api';
import { ActionStore } from '@shared/actions/action.store';
import { DetailsMode } from '@shared/forms/with-details-mode';
import { RichTextImageLinks } from '@shared/rich-text/file-image/rich-text-image-links';
import { EmployeeStore } from '@shared/employee/employee.store';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { Temporal } from '@shared/time/temporal';
import { Permission } from '@shared/security/permissions';

import { FilesApi } from '@files/files-api';
import {
  type LookedUpFile,
  type WorkOrder,
  WorkOrderCategory,
  WorkOrderPriority,
  WorkOrderStatus,
} from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import type { WorkOrderFormValue } from '@work-orders/work-order-schema';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import { WorkOrderDrawerStore } from './work-order-drawer.store';
import { fileEntries, toCreateBody, toUpdateBody } from './work-order-drawer.store.utils';

const WORK_ORDER: WorkOrder = {
  id: 'wo-1',
  title: 'Replace filter',
  description: null,
  priority: WorkOrderPriority.High,
  category: WorkOrderCategory.Repair,
  estimateMinutes: 60,
  status: WorkOrderStatus.Staged,
  assigneeId: 'e1',
  assigneeName: 'Ann Lee',
  plannedStartLocal: '2027-03-28T09:00:00',
  plannedStartUtc: '2027-03-28T06:00:00Z',
  isDelayed: false,
  machineId: null,
  machineName: null,
  isMachineDeleted: false,
  fileIds: ['f1', 'f2'],
  rowVersion: 5,
  createdAt: '2027-03-01T08:00:00Z',
};

const FORM_VALUE: WorkOrderFormValue = {
  title: 'Replace filter',
  description: null,
  priority: WorkOrderPriority.High,
  category: WorkOrderCategory.Repair,
  machineId: null,
  estimateMinutes: 90,
  fileIds: ['f2', 'f3'],
  assigneeId: 'e1',
  start: Temporal.PlainDateTime.from('2027-03-28T09:00:00'),
};

function lookedUp(id: string): LookedUpFile {
  return { id, fileName: id, extension: 'pdf', contentType: 'application/pdf', sizeBytes: 10, machineId: null };
}

describe('toUpdateBody', () => {
  it('toUpdateBody_WithoutManagePermission_KeepsTheStoredEstimate', () => {
    const body = toUpdateBody(FORM_VALUE, WORK_ORDER, false);

    expect(body.estimateMinutes).toBe(60);
    expect(body.rowVersion).toBe(5);
  });

  it('toUpdateBody_WithManagePermission_SendsTheEditedEstimate', () => {
    const body = toUpdateBody(FORM_VALUE, WORK_ORDER, true);

    expect(body.estimateMinutes).toBe(90);
  });
});

describe('toUpdateBody schedule', () => {
  it.each([
    { name: 'NoChange', value: FORM_VALUE, assigneeId: 'e1', plannedStartLocal: '2027-03-28T09:00:00' },
    {
      name: 'Move',
      value: { ...FORM_VALUE, start: Temporal.PlainDateTime.from('2027-03-29T10:00:00') },
      assigneeId: 'e1',
      plannedStartLocal: '2027-03-29T10:00:00',
    },
    {
      name: 'AnotherAssignee',
      value: { ...FORM_VALUE, assigneeId: 'e2' },
      assigneeId: 'e2',
      plannedStartLocal: '2027-03-28T09:00:00',
    },
    {
      name: 'Unassign',
      value: { ...FORM_VALUE, assigneeId: null, start: null },
      assigneeId: null,
      plannedStartLocal: null,
    },
  ])('toUpdateBody_$name_SendsTheAssigneeAndTheStart', ({ value, assigneeId, plannedStartLocal }) => {
    const body = toUpdateBody(value, WORK_ORDER, true);

    expect(body.assigneeId).toBe(assigneeId);
    expect(body.plannedStartLocal).toBe(plannedStartLocal);
  });

  it('toUpdateBody_OfAWorker_SendsTheCurrentSlotNotTheEditedOne', () => {
    const edited = { ...FORM_VALUE, assigneeId: 'e2', start: Temporal.PlainDateTime.from('2027-04-01T10:00:00') };

    const body = toUpdateBody(edited, WORK_ORDER, false);

    expect(body.assigneeId).toBe('e1');
    expect(body.plannedStartLocal).toBe('2027-03-28T09:00:00');
  });

  it('toUpdateBody_OfABacklogWorkOrderWithoutSlot_SendsBothNull', () => {
    const backlog = { ...WORK_ORDER, assigneeId: null, plannedStartLocal: null };

    const body = toUpdateBody({ ...FORM_VALUE, assigneeId: null, start: null }, backlog, true);

    expect(body.assigneeId).toBeNull();
    expect(body.plannedStartLocal).toBeNull();
  });
});

describe('toCreateBody schedule', () => {
  it('toCreateBody_WithAssigneeAndStart_SendsBoth', () => {
    const body = toCreateBody(FORM_VALUE);

    expect(body.assigneeId).toBe('e1');
    expect(body.plannedStartLocal).toBe('2027-03-28T09:00:00');
  });

  it('toCreateBody_WithoutAssignee_SendsNeither', () => {
    const body = toCreateBody({ ...FORM_VALUE, assigneeId: null, start: null });

    expect(body).not.toHaveProperty('assigneeId');
    expect(body).not.toHaveProperty('plannedStartLocal');
  });
});

describe('fileEntries', () => {
  it('fileEntries_WithFileMissingInLookup_KeepsItAsUnavailable', () => {
    const entries = fileEntries(['f1', 'f2'], [lookedUp('f1')]);

    expect(entries).toEqual([
      { id: 'f1', file: lookedUp('f1') },
      { id: 'f2', file: null },
    ]);
  });
});

describe('WorkOrderDrawerStore', () => {
  const get = vi.fn();
  const update = vi.fn();
  const create = vi.fn();
  const complete = vi.fn();
  const notify = vi.fn();
  const add = vi.fn();
  const dispatch = vi.fn();
  const history = vi.fn();
  const cancelAction = vi.fn();

  async function setup(
    options: { canManage?: boolean; canWrite?: boolean; employeeId?: string | null; isUpdate?: boolean } = {},
  ): Promise<InstanceType<typeof WorkOrderDrawerStore>> {
    TestBed.configureTestingModule({
      providers: [
        WorkOrderDrawerStore,
        {
          provide: WorkOrdersApi,
          useValue: {
            get,
            update,
            create,
            history,
            lookupFiles: () => of({ files: [] }),
          },
        },
        { provide: FilesApi, useValue: { search: () => of({ items: [], totalCount: 0 }), upload: vi.fn() } },
        {
          provide: AuthStore,
          useValue: {
            hasPermission: (permission: string) =>
              permission === Permission.WorkOrdersWrite ? (options.canWrite ?? false) : (options.canManage ?? false),
          },
        },
        { provide: EmployeeStore, useValue: { id: () => options.employeeId ?? null } },
        { provide: LookupsApi, useValue: { machines: () => of([]) } },
        { provide: ActionStore, useValue: { complete, notify, cancel: cancelAction, dispatch } },
        { provide: RichTextImageLinks, useValue: { resolve: () => of(new Map<string, string>()) } },
        { provide: MessageService, useValue: { add } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
      ],
    });
    const store = TestBed.inject(WorkOrderDrawerStore);
    store.start({
      type: WorkOrderActionType.Open,
      payload: options.isUpdate === false ? {} : { workOrderId: 'wo-1' },
    });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    return store;
  }

  beforeEach(() => {
    get.mockReset().mockReturnValue(of(WORK_ORDER));
    update.mockReset().mockReturnValue(of(undefined));
    create.mockReset().mockReturnValue(of({ id: 'new' }));
    complete.mockReset();
    notify.mockReset();
    dispatch.mockReset();
    cancelAction.mockReset();
    history.mockReset().mockReturnValue(of({ items: [], totalCount: 0, nextAfter: null }));
    add.mockReset();
  });

  it('start_WhenTheWorkOrderIsNotFound_ClosesTheDrawer', async () => {
    get.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.NotFound })));

    await setup();

    expect(cancelAction).toHaveBeenCalledTimes(1);
  });

  it('start_WhenTheWorkOrderLoads_KeepsTheDrawerOpen', async () => {
    await setup();

    expect(cancelAction).not.toHaveBeenCalled();
  });

  it('save_OnUpdate_SendsTheNewFileListAndTheRowVersion', async () => {
    const store = await setup({ canManage: true });

    store.save(FORM_VALUE);

    expect(update).toHaveBeenCalledWith(
      'wo-1',
      expect.objectContaining({ fileIds: ['f2', 'f3'], rowVersion: 5, estimateMinutes: 90 }),
      expect.anything(),
    );
    expect(notify).toHaveBeenCalledWith(WorkOrderActionType.Saved);
    expect(complete).not.toHaveBeenCalled();
  });

  it('start_ForExisting_IsViewMode', async () => {
    const store = await setup();

    expect(store.mode()).toBe(DetailsMode.View);
  });

  it('start_ForCreate_IsEditMode', async () => {
    const store = await setup({ isUpdate: false });

    expect(store.mode()).toBe(DetailsMode.Edit);
  });

  it('isEditShown_WithoutWritePermission_IsFalse', async () => {
    const store = await setup({ canWrite: false });

    expect(store.isEditShown()).toBe(false);
  });

  it('isEditShown_WithWritePermissionAfterLoad_IsTrue', async () => {
    const store = await setup({ canWrite: true });

    expect(store.isEditShown()).toBe(true);
  });

  it('save_OnUpdate_NotifiesReloadsAndReturnsToView', async () => {
    const store = await setup({ canManage: true });
    store.edit();
    const getCallsBefore = get.mock.calls.length;

    store.save(FORM_VALUE);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(notify).toHaveBeenCalledTimes(1);
    expect(get.mock.calls.length - getCallsBefore).toBe(1);
    expect(store.mode()).toBe(DetailsMode.View);
    expect(store.isSaving()).toBe(false);
  });

  it('save_OnUpdateTwice_SendsTheReloadedRowVersion', async () => {
    const store = await setup({ canManage: true });
    store.edit();
    store.save(FORM_VALUE);
    get.mockReturnValue(of({ ...WORK_ORDER, rowVersion: 6 }));
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    store.edit();

    store.save(FORM_VALUE);

    expect(update.mock.calls.map(([, body]) => (body as { rowVersion: number }).rowVersion)).toEqual([5, 6]);
  });

  it('cancelEdit_ForExisting_ReturnsToView', async () => {
    const store = await setup();
    store.edit();

    store.cancelEdit();

    expect(store.mode()).toBe(DetailsMode.View);
  });

  it('save_OnUpdateWithoutManagePermission_SendsTheStoredEstimate', async () => {
    const store = await setup({ canManage: false });

    store.save(FORM_VALUE);

    expect(update).toHaveBeenCalledWith('wo-1', expect.objectContaining({ estimateMinutes: 60 }), expect.anything());
  });

  it('save_OnCreate_CallsCreateWithoutAssignee', async () => {
    const store = await setup({ isUpdate: false });

    store.save({ ...FORM_VALUE, assigneeId: null, start: null });

    expect(create).toHaveBeenCalledTimes(1);
    expect(create.mock.calls[0][0]).not.toHaveProperty('assigneeId');
  });

  it('save_OnCreateWithAssigneeAndStart_CallsCreateWithBoth', async () => {
    const store = await setup({ isUpdate: false });

    store.save(FORM_VALUE);

    expect(create.mock.calls[0][0]).toMatchObject({
      assigneeId: 'e1',
      plannedStartLocal: '2027-03-28T09:00:00',
    });
  });

  it('save_OnCreate_CompletesWithTheBacklogStatusOfTheNewWorkOrder', async () => {
    const store = await setup({ isUpdate: false });

    store.save(FORM_VALUE);

    expect(complete).toHaveBeenCalledWith(WorkOrderActionType.Saved, { status: 'backlog' });
  });

  it('history_BeforeTheHistoryTabOpens_SendsNoRequest', async () => {
    await setup({ canManage: true });

    expect(history).not.toHaveBeenCalled();
  });

  it('activateHistory_AfterTheDrawerOpened_SendsOneRequest', async () => {
    const store = await setup({ canManage: true });

    store.activateHistory();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(history).toHaveBeenCalledTimes(1);
  });

  it('activateHistory_OnOpen_LoadsTheFirstPageWithTenRowsAndNoAfter', async () => {
    const store = await setup({ canManage: true });

    store.activateHistory();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(history).toHaveBeenCalledWith('wo-1', 10, undefined);
  });

  it('nextHistoryPage_ThenPrevious_SendsTheAfterOfThePageAndGoesBackWithoutAnotherQuery', async () => {
    const store = await setup({ canManage: true });
    store.activateHistory();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    store.nextHistoryPage(14);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    const pageIndexAfterNext = store.historyPageIndex();
    store.previousHistoryPage();
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(history).toHaveBeenNthCalledWith(2, 'wo-1', 10, 14);
    expect(pageIndexAfterNext).toBe(1);
    expect(store.historyPageIndex()).toBe(0);
    expect(history).toHaveBeenLastCalledWith('wo-1', 10, undefined);
  });

  it('save_OnAnEditFromTheSecondHistoryPage_ReloadsTheFirstPage', async () => {
    const store = await setup({ canManage: true });
    store.activateHistory();
    store.nextHistoryPage(14);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    history.mockClear();

    store.save(FORM_VALUE);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.historyPageIndex()).toBe(0);
    expect(history).toHaveBeenCalledWith('wo-1', 10, undefined);
  });

  it.each(
    [
      { name: 'ForASupervisor', options: { canManage: true } },
      { name: 'ForAWorkerWithTheOwnWorkOrder', options: { canWrite: true, employeeId: 'e1' } },
    ].map((row) => [row.name, row] as [string, typeof row]),
  )('canChangeStatus_%s_IsTrue', async (_name, { options }) => {
    const store = await setup(options);

    expect(store.canChangeStatus()).toBe(true);
  });

  it('canChangeStatus_InEditMode_IsFalse', async () => {
    const store = await setup({ canManage: true });
    store.edit();

    expect(store.canChangeStatus()).toBe(false);
  });

  it('requestChangeStatus_WhenTheDialogReturns_IsViewMode', async () => {
    const store = await setup({ canManage: true });
    store.edit();
    store.requestChangeStatus();

    store.start({ type: WorkOrderActionType.Open, payload: { workOrderId: 'wo-1' } });

    expect(store.mode()).toBe(DetailsMode.View);
  });

  it('canChangeStatus_ForAWorkerWithAnotherAssignee_IsFalse', async () => {
    const store = await setup({ canWrite: true, employeeId: 'someone-else' });

    expect(store.canChangeStatus()).toBe(false);
  });

  it('requestChangeStatus_WithAWorkOrder_DispatchesTheActionAndReturnsToTheDrawer', async () => {
    const store = await setup({ canManage: true });

    store.requestChangeStatus();

    expect(dispatch).toHaveBeenCalledWith(
      {
        type: WorkOrderActionType.ChangeStatus,
        payload: { workOrderId: WORK_ORDER.id, rowVersion: WORK_ORDER.rowVersion },
      },
      { type: WorkOrderActionType.Open, payload: { workOrderId: 'wo-1' } },
    );
  });

  it('save_With409_ShowsTheConflictToastAndReloadsTheWorkOrder', async () => {
    update.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = await setup({ canManage: true });
    const callsBefore = get.mock.calls.length;

    store.save(FORM_VALUE);
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(add).toHaveBeenCalledWith(
      expect.objectContaining({ severity: 'error', detail: 'WORK_ORDERS.TOASTS.CONFLICT' }),
    );
    expect(get.mock.calls.length - callsBefore).toBe(1);
    expect(complete).not.toHaveBeenCalled();
  });

  it('save_With409_StaysInEditModeAndReloads', async () => {
    update.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = await setup({ canManage: true });
    store.edit();

    store.save(FORM_VALUE);

    expect(store.mode()).toBe(DetailsMode.Edit);
    expect(notify).not.toHaveBeenCalled();
  });

  it('save_With422_ShowsTheRuleToastAndKeepsTheDrawerOpen', async () => {
    update.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.UnprocessableEntity })));
    const store = await setup({ canManage: true });

    store.save(FORM_VALUE);

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error', detail: 'WORK_ORDERS.TOASTS.RULE' }));
    expect(complete).not.toHaveBeenCalled();
  });

  it('save_With422_StaysInEditMode', async () => {
    update.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.UnprocessableEntity })));
    const store = await setup({ canManage: true });
    store.edit();

    store.save(FORM_VALUE);

    expect(store.mode()).toBe(DetailsMode.Edit);
    expect(notify).not.toHaveBeenCalled();
  });
});
