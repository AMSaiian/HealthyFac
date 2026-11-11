import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { LookupsApi } from '@shared/lookups/lookups-api';
import { ActionStore } from '@shared/actions/action.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import {
  type DaySchedule,
  type WorkOrder,
  WorkOrderCategory,
  WorkOrderPriority,
  WorkOrderStatus,
} from '@work-orders/work-order';
import { WorkOrderActionType } from '@work-orders/work-order-actions';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import { ScheduleActionsStore } from './schedule-actions.store';

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
  plannedStartLocal: '2027-06-01T09:00:00',
  plannedStartUtc: '2027-06-01T06:00:00Z',
  isDelayed: false,
  machineId: null,
  machineName: null,
  isMachineDeleted: false,
  fileIds: [],
  rowVersion: 5,
  createdAt: '2027-05-01T08:00:00Z',
};

const DAY: DaySchedule = {
  date: '2027-06-01',
  timeZoneId: 'Europe/Kyiv',
  source: 'template',
  blocks: [
    { startLocal: '08:00', endLocal: '12:00', startUtc: '2027-06-01T05:00:00Z', endUtc: '2027-06-01T09:00:00Z' },
  ],
  workOrders: [
    {
      id: 'other',
      title: 'Other',
      status: WorkOrderStatus.Staged,
      priority: WorkOrderPriority.Low,
      plannedStartLocal: '2027-06-01T08:00:00',
      plannedStartUtc: '2027-06-01T05:00:00Z',
      estimateMinutes: 60,
      occupancyEndUtc: '2027-06-01T06:00:00Z',
      isDelayed: false,
      rowVersion: 1,
    },
  ],
};

describe('ScheduleActionsStore', () => {
  const get = vi.fn();
  const assign = vi.fn();
  const move = vi.fn();
  const unassign = vi.fn();
  const changeStatus = vi.fn();
  const remove = vi.fn();
  const daySchedule = vi.fn();
  const complete = vi.fn();
  const cancel = vi.fn();
  const add = vi.fn();
  const confirm = vi.fn();

  async function setup(
    type: string,
    options: { isConfirmed?: boolean } = {},
  ): Promise<InstanceType<typeof ScheduleActionsStore>> {
    TestBed.configureTestingModule({
      providers: [
        ScheduleActionsStore,
        {
          provide: WorkOrdersApi,
          useValue: {
            get,
            assign,
            move,
            unassign,
            changeStatus,
            delete: remove,
            daySchedule,
          },
        },
        { provide: LookupsApi, useValue: { employees: () => of([]) } },
        { provide: ActionStore, useValue: { complete, cancel } },
        {
          provide: ConfirmationService,
          useValue: {
            confirm: confirm.mockImplementation((confirmation: { accept?: () => void; reject?: () => void }) =>
              options.isConfirmed === true ? confirmation.accept?.() : confirmation.reject?.(),
            ),
          },
        },
        { provide: MessageService, useValue: { add } },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2027-06-01T04:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(ScheduleActionsStore);
    store.start({ type, payload: { workOrderId: 'wo-1', rowVersion: 5 } });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
    return store;
  }

  beforeEach(() => {
    get.mockReset().mockReturnValue(of(WORK_ORDER));
    for (const mock of [assign, move, unassign, changeStatus, remove]) {
      mock.mockReset().mockReturnValue(of(undefined));
    }
    daySchedule.mockReset().mockReturnValue(of(DAY));
    complete.mockReset();
    cancel.mockReset();
    add.mockReset();
    confirm.mockClear();
  });

  it('submitSchedule_OnAssign_SendsAssigneeStartAndRowVersion', async () => {
    const store = await setup(WorkOrderActionType.Assign);

    store.submitSchedule({ assigneeId: 'e2', start: Temporal.PlainDateTime.from('2027-06-01T09:00') });

    expect(assign).toHaveBeenCalledWith(
      'wo-1',
      { assigneeId: 'e2', plannedStartLocal: '2027-06-01T09:00:00', rowVersion: 5 },
      expect.anything(),
    );
    expect(complete).toHaveBeenCalledWith(WorkOrderActionType.Saved);
  });

  it('submitSchedule_OnMove_CallsMove', async () => {
    const store = await setup(WorkOrderActionType.Move);

    store.submitSchedule({ assigneeId: 'e1', start: Temporal.PlainDateTime.from('2027-06-01T10:00') });

    expect(move).toHaveBeenCalledTimes(1);
    expect(assign).not.toHaveBeenCalled();
  });

  it('submitSchedule_With409_ShowsTheToastAndCompletesWithConflict', async () => {
    assign.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = await setup(WorkOrderActionType.Assign);

    store.submitSchedule({ assigneeId: 'e2', start: Temporal.PlainDateTime.from('2027-06-01T09:00') });

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'WORK_ORDERS.TOASTS.CONFLICT' }));
    expect(complete).toHaveBeenCalledWith(WorkOrderActionType.Conflict);
  });

  it('submitSchedule_With422_ShowsTheRuleToastAndKeepsTheDialogOpen', async () => {
    assign.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.UnprocessableEntity })));
    const store = await setup(WorkOrderActionType.Assign);

    store.submitSchedule({ assigneeId: 'e2', start: Temporal.PlainDateTime.from('2027-06-01T09:00') });

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'WORK_ORDERS.TOASTS.RULE' }));
    expect(complete).not.toHaveBeenCalled();
    expect(cancel).not.toHaveBeenCalled();
  });

  it('requestConfirmation_OnUnassignWhenConfirmed_UnassignsWithTheRowVersion', async () => {
    const store = await setup(WorkOrderActionType.Unassign, { isConfirmed: true });

    store.requestConfirmation();

    expect(confirm).toHaveBeenCalledWith(expect.objectContaining({ acceptButtonProps: { severity: 'danger' } }));
    expect(unassign).toHaveBeenCalledWith('wo-1', 5, expect.anything());
  });

  it('requestConfirmation_CalledTwice_OpensTheConfirmationOnce', async () => {
    const store = await setup(WorkOrderActionType.Unassign, { isConfirmed: false });

    store.requestConfirmation();
    store.requestConfirmation();

    expect(confirm).toHaveBeenCalledTimes(1);
  });

  it('requestConfirmation_OnDeleteWhenConfirmed_SkipsTheInterceptorToast', async () => {
    const store = await setup(WorkOrderActionType.Delete, { isConfirmed: true });

    store.requestConfirmation();

    expect(remove).toHaveBeenCalledWith('wo-1', 5, expect.objectContaining({ context: expect.anything() }));
  });

  it('requestConfirmation_OnDeleteWhenRejected_CallsNoEndpointAndCancels', async () => {
    const store = await setup(WorkOrderActionType.Delete, { isConfirmed: false });

    store.requestConfirmation();

    expect(remove).not.toHaveBeenCalled();
    expect(cancel).toHaveBeenCalledTimes(1);
  });

  it('requestConfirmation_OnDeleteWith422_ShowsTheRuleToastAndCancels', async () => {
    remove.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.UnprocessableEntity })));
    const store = await setup(WorkOrderActionType.Delete, { isConfirmed: true });

    store.requestConfirmation();

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'WORK_ORDERS.TOASTS.RULE' }));
    expect(cancel).toHaveBeenCalledTimes(1);
  });

  it('submitStatus_OnStatusChange_SendsTheStatusAndTheRowVersion', async () => {
    const store = await setup(WorkOrderActionType.ChangeStatus);

    store.submitStatus(WorkOrderStatus.InProgress);

    expect(changeStatus).toHaveBeenCalledWith(
      'wo-1',
      { status: WorkOrderStatus.InProgress, rowVersion: 5 },
      expect.anything(),
    );
  });

  it('statusOptions_Always_LeaveOutTheCurrentStatusAndBacklog', async () => {
    const store = await setup(WorkOrderActionType.ChangeStatus);

    const options = store.statusOptions();

    expect(options).not.toContain(WorkOrderStatus.Staged);
    expect(options).not.toContain(WorkOrderStatus.Backlog);
    expect(options).toContain(WorkOrderStatus.InProgress);
  });

  it('suggestion_OnAMoveInsideItsOwnOldSlot_GivesThatStart', async () => {
    daySchedule.mockReturnValue(
      of({
        ...DAY,
        workOrders: [
          {
            ...DAY.workOrders[0],
            id: 'wo-1',
            plannedStartUtc: '2027-06-01T05:00:00Z',
            occupancyEndUtc: '2027-06-01T06:00:00Z',
          },
        ],
      }),
    );
    const store = await setup(WorkOrderActionType.Move);

    store.select({ assigneeId: 'e1', date: Temporal.PlainDate.from('2027-06-01') });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.suggestion()?.toString()).toBe('2027-06-01T08:00:00');
  });

  it('suggestion_WithSelection_IsTheFirstFreeStartOfTheDay', async () => {
    const store = await setup(WorkOrderActionType.Assign);

    store.select({ assigneeId: 'e2', date: Temporal.PlainDate.from('2027-06-01') });
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();

    expect(store.suggestion()?.toString()).toBe('2027-06-01T09:00:00');
    expect(store.hasNoFreeStart()).toBe(false);
  });
});
