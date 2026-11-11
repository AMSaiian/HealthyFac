import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { LookupsApi } from '@shared/lookups/lookups-api';
import { ActionStore } from '@shared/actions/action.store';
import { EmployeeStore } from '@shared/employee/employee.store';
import { AuthStore } from '@shared/security/auth.store';
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
import { fileEntries, toUpdateBody } from './work-order-drawer.store.utils';

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
        { provide: ActionStore, useValue: { complete, cancel: cancelAction, dispatch } },
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
    dispatch.mockReset();
    cancelAction.mockReset();
    history.mockReset().mockReturnValue(of([]));
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
    expect(complete).toHaveBeenCalledWith(WorkOrderActionType.Saved, undefined);
  });

  it('save_OnUpdateWithoutManagePermission_SendsTheStoredEstimate', async () => {
    const store = await setup({ canManage: false });

    store.save(FORM_VALUE);

    expect(update).toHaveBeenCalledWith('wo-1', expect.objectContaining({ estimateMinutes: 60 }), expect.anything());
  });

  it('save_OnCreate_CallsCreateWithoutAssignee', async () => {
    const store = await setup({ isUpdate: false });

    store.save(FORM_VALUE);

    expect(create).toHaveBeenCalledTimes(1);
    expect(create.mock.calls[0][0]).not.toHaveProperty('assigneeId');
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

  it('canChangeStatus_ForASupervisor_IsTrue', async () => {
    const store = await setup({ canManage: true });

    expect(store.canChangeStatus()).toBe(true);
  });

  it('canChangeStatus_ForAWorkerWithTheOwnWorkOrder_IsTrue', async () => {
    const store = await setup({ canWrite: true, employeeId: 'e1' });

    expect(store.canChangeStatus()).toBe(true);
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

  it('save_With422_ShowsTheRuleToastAndKeepsTheDrawerOpen', async () => {
    update.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.UnprocessableEntity })));
    const store = await setup({ canManage: true });

    store.save(FORM_VALUE);

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ severity: 'error', detail: 'WORK_ORDERS.TOASTS.RULE' }));
    expect(complete).not.toHaveBeenCalled();
  });
});
