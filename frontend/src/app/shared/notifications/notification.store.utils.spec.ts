import {
  jumpOf,
  messageSeverityOf,
  minutesOfTimeSpan,
  type NotificationJump,
  type TextContext,
  textKeyOf,
  textParamsOf,
  toastIconOf,
} from './notification.store.utils';
import { type RealtimeNotification, RealtimeNotificationKind } from '@shared/realtime/realtime';

import { FileActionType } from '@files/file-actions';
import { WorkOrderActionType } from '@work-orders/work-order-actions';

function notificationOf(kind: RealtimeNotificationKind, data: unknown): RealtimeNotification {
  return {
    notificationId: 'n-1',
    kind,
    occurredAt: '2026-11-21T10:00:00Z',
    actorSubject: null,
    data,
  } as RealtimeNotification;
}

const KNOWN_KEYS = new Set([
  'NOTIFICATIONS.PROVISIONING_REASONS.EMAIL_ALREADY_REGISTERED',
  'NOTIFICATIONS.PROVISIONING_REASONS.UNKNOWN',
  'INGEST.STATES.COMPLETED',
  'MAINTENANCE.SEVERITIES.ALARM',
]);

/** A fake translation: a known key gives `[KEY]`, an unknown key gives itself, as ngx-translate does. */
const CONTEXT: TextContext = {
  language: 'en',
  translate: (key) => (KNOWN_KEYS.has(key) ? `[${key}]` : key),
};

const JUMP_CASES: readonly [RealtimeNotificationKind, unknown, NotificationJump | null][] = [
  [
    RealtimeNotificationKind.ShiftChanged,
    { employeeId: 'e-1', date: '2026-11-21' },
    { type: 'route', commands: ['/employees', 'e-1', 'schedule'], queryParams: { date: '2026-11-21' } },
  ],
  [
    RealtimeNotificationKind.WorkOrderAssigned,
    { workOrderId: 'w-1', title: 'T', plannedStartLocal: '2026-11-21T09:00:00' },
    { type: 'action', action: { type: WorkOrderActionType.Open, payload: { workOrderId: 'w-1' } } },
  ],
  [RealtimeNotificationKind.WorkOrderUnassigned, { title: 'T' }, null],
  [
    RealtimeNotificationKind.WorkOrderEstimateChanged,
    { workOrderId: 'w-2', title: 'T', oldEstimate: '01:00:00', newEstimate: '01:30:00' },
    { type: 'action', action: { type: WorkOrderActionType.Open, payload: { workOrderId: 'w-2' } } },
  ],
  [
    RealtimeNotificationKind.WorkOrderRescheduled,
    { workOrderId: 'w-3', title: 'T', plannedStartLocal: '2026-11-21T09:00:00' },
    { type: 'action', action: { type: WorkOrderActionType.Open, payload: { workOrderId: 'w-3' } } },
  ],
  [RealtimeNotificationKind.WorkOrderReturnedToBacklog, { title: 'T' }, null],
  [
    RealtimeNotificationKind.IngestFinished,
    { attemptId: 'a-1', fileName: 'manual.pdf', state: 'completed' },
    { type: 'action', action: { type: FileActionType.IngestSummary, payload: { attemptId: 'a-1' } } },
  ],
  [
    RealtimeNotificationKind.WorkOrderBlocked,
    { workOrderId: 'w-4', title: 'T' },
    { type: 'action', action: { type: WorkOrderActionType.Open, payload: { workOrderId: 'w-4' } } },
  ],
  [
    RealtimeNotificationKind.WorkOrderDraftCreated,
    { draftId: 'd-1', title: 'T' },
    { type: 'route', commands: ['/work-orders/drafts'], queryParams: { draftId: 'd-1' } },
  ],
  [
    RealtimeNotificationKind.ProvisioningFailed,
    { employeeName: 'Ann Lee', reason: 'refused' },
    { type: 'route', commands: ['/employees'] },
  ],
  [
    RealtimeNotificationKind.MaintenanceCaseOpened,
    { caseId: 'c-1', severity: 'alarm' },
    { type: 'route', commands: ['/maintenance/cases', 'c-1'] },
  ],
];

describe('notification.store.utils', () => {
  it.each(JUMP_CASES)('jumpOf_For%s_GivesItsTarget', (kind, data, expected) => {
    expect(jumpOf(notificationOf(kind, data))).toEqual(expected);
  });

  it('jumpOf_ForEveryKind_IsCovered', () => {
    expect(JUMP_CASES.map(([kind]) => kind).sort()).toEqual(Object.values(RealtimeNotificationKind).sort());
  });

  it('textKeyOf_WithCamelCaseKind_GivesConstantCaseKey', () => {
    expect(textKeyOf(RealtimeNotificationKind.WorkOrderReturnedToBacklog)).toBe(
      'NOTIFICATIONS.KINDS.WORK_ORDER_RETURNED_TO_BACKLOG',
    );
    expect(textKeyOf(RealtimeNotificationKind.ShiftChanged)).toBe('NOTIFICATIONS.KINDS.SHIFT_CHANGED');
  });

  it('minutesOfTimeSpan_WithDaysHoursAndMinutes_GivesWholeMinutes', () => {
    expect(minutesOfTimeSpan('01:30:00')).toBe(90);
    expect(minutesOfTimeSpan('1.02:30:00')).toBe(1590);
    expect(minutesOfTimeSpan('not a span')).toBe('not a span');
  });

  it('textParamsOf_ForEstimateChange_AddsTheEstimatesInMinutes', () => {
    const params = textParamsOf(
      notificationOf(RealtimeNotificationKind.WorkOrderEstimateChanged, {
        workOrderId: 'w-2',
        title: 'T',
        oldEstimate: '01:00:00',
        newEstimate: '01:30:00',
      }),
      CONTEXT,
    );

    expect(params).toMatchObject({ title: 'T', oldMinutes: 60, newMinutes: 90 });
  });

  it('textParamsOf_ForShiftChanged_FormatsTheDateInTheLanguage', () => {
    const params = textParamsOf(
      notificationOf(RealtimeNotificationKind.ShiftChanged, { employeeId: 'e-1', date: '2026-11-21' }),
      CONTEXT,
    );

    expect(params['date']).toBe('Nov 21, 2026');
  });

  it.each([RealtimeNotificationKind.WorkOrderAssigned, RealtimeNotificationKind.WorkOrderRescheduled])(
    'textParamsOf_For%s_FormatsThePlannedLocalTime',
    (kind) => {
      const params = textParamsOf(
        notificationOf(kind, { workOrderId: 'w-1', title: 'T', plannedStartLocal: '2026-11-21T08:00:00' }),
        CONTEXT,
      );

      expect(params['plannedStartLocal']).toBe('Nov 21, 2026, 08:00');
    },
  );

  it('textParamsOf_ForIngestFinished_TranslatesTheState', () => {
    const params = textParamsOf(
      notificationOf(RealtimeNotificationKind.IngestFinished, {
        attemptId: 'a-1',
        fileName: 'manual.pdf',
        state: 'completed',
      }),
      CONTEXT,
    );

    expect(params).toMatchObject({ fileName: 'manual.pdf', state: '[INGEST.STATES.COMPLETED]' });
  });

  it('textParamsOf_ForMaintenanceCaseOpened_TranslatesTheSeverity', () => {
    const params = textParamsOf(
      notificationOf(RealtimeNotificationKind.MaintenanceCaseOpened, { caseId: 'c-1', severity: 'alarm' }),
      CONTEXT,
    );

    expect(params['severity']).toBe('[MAINTENANCE.SEVERITIES.ALARM]');
  });

  it('textParamsOf_ForProvisioningFailed_TranslatesAKnownReason', () => {
    const params = textParamsOf(
      notificationOf(RealtimeNotificationKind.ProvisioningFailed, {
        employeeName: 'Ann Lee',
        reason: 'EmailAlreadyRegistered',
      }),
      CONTEXT,
    );

    expect(params).toMatchObject({
      employeeName: 'Ann Lee',
      reason: '[NOTIFICATIONS.PROVISIONING_REASONS.EMAIL_ALREADY_REGISTERED]',
    });
  });

  it('textParamsOf_ForProvisioningFailed_WithAnUnknownReason_UsesTheUnknownText', () => {
    const params = textParamsOf(
      notificationOf(RealtimeNotificationKind.ProvisioningFailed, { employeeName: 'Ann Lee', reason: 'Strange' }),
      CONTEXT,
    );

    expect(params['reason']).toBe('[NOTIFICATIONS.PROVISIONING_REASONS.UNKNOWN]');
  });

  it.each([
    ['success', 'pi pi-check-circle'],
    ['error', 'pi pi-times-circle'],
    [undefined, 'pi pi-info-circle'],
  ])('toastIconOf_With%s_Gives%s', (severity, expected) => {
    expect(toastIconOf(severity)).toBe(expected);
  });

  it.each([
    [false, 'info'],
    [true, 'secondary'],
  ])('messageSeverityOf_WithIsRead%s_Gives%s', (isRead, expected) => {
    const entry = { notification: notificationOf(RealtimeNotificationKind.WorkOrderAssigned, {}), isRead };

    expect(messageSeverityOf(entry)).toBe(expected);
  });
});
