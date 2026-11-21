import type { AppAction } from '@shared/actions/app-action';
import {
  type IngestFinishedData,
  type MaintenanceCaseOpenedData,
  type ProvisioningFailedData,
  type RealtimeNotification,
  RealtimeNotificationKind,
  type ShiftChangedData,
  type WorkOrderAssignedData,
  type WorkOrderBlockedData,
  type WorkOrderDraftCreatedData,
  type WorkOrderEstimateChangedData,
  type WorkOrderRescheduledData,
} from '@shared/realtime/realtime';

import { Temporal } from '@shared/time/temporal';

import { FileActionType } from '@files/file-actions';
import { WorkOrderActionType } from '@work-orders/work-order-actions';

export const MAX_NOTIFICATION_ENTRIES = 100;
export const NOTIFICATION_TOAST_KEY = 'notifications';
export const NOTIFICATION_TOAST_LIFE_MILLISECONDS = 8000;

const MINUTES_PER_HOUR = 60;
const HOURS_PER_DAY = 24;
const TIME_SPAN_PATTERN = /^(?:(\d+)\.)?(\d+):(\d+):(\d+)(?:\.\d+)?$/;
const TEXT_KEY_PREFIX = 'NOTIFICATIONS.KINDS.';
const CAMEL_BOUNDARY = /([a-z])([A-Z])/g;
const INGEST_STATE_KEY_PREFIX = 'INGEST.STATES.';
const SEVERITY_KEY_PREFIX = 'MAINTENANCE.SEVERITIES.';
const PROVISIONING_REASON_KEY_PREFIX = 'NOTIFICATIONS.PROVISIONING_REASONS.';
const UNKNOWN_REASON_KEY = `${PROVISIONING_REASON_KEY_PREFIX}UNKNOWN`;

export interface NotificationEntry {
  readonly notification: RealtimeNotification;
  readonly isOpened: boolean;
}

/** An entry with its text, which the bell and the toast show. */
export interface NotificationItem {
  readonly entry: NotificationEntry;
  readonly text: string;
}

/** What the text of a notification needs from the app: the translation of a key and the language for the dates. */
export interface TextContext {
  readonly translate: (key: string) => string;
  readonly language: string;
}

export type NotificationJump =
  | { readonly type: 'route'; readonly commands: readonly string[]; readonly queryParams?: Record<string, string> }
  | { readonly type: 'action'; readonly action: AppAction };

/** The translation key of the text of a kind: `shiftChanged` gives `NOTIFICATIONS.KINDS.SHIFT_CHANGED`. */
export function textKeyOf(kind: RealtimeNotificationKind): string {
  return `${TEXT_KEY_PREFIX}${constantCase(kind)}`;
}

function constantCase(value: string): string {
  return value.replace(CAMEL_BOUNDARY, '$1_$2').toUpperCase();
}

/** Turns a time span of the backend (`1.02:30:00`) into whole minutes. A text that is no time span stays as it is. */
export function minutesOfTimeSpan(value: string): number | string {
  const match = TIME_SPAN_PATTERN.exec(value);
  if (match === null) {
    return value;
  }
  const [, days, hours, minutes] = match;
  return (Number(days ?? 0) * HOURS_PER_DAY + Number(hours)) * MINUTES_PER_HOUR + Number(minutes);
}

function formatDate(value: string, language: string): string {
  return Temporal.PlainDate.from(value).toLocaleString(language, { dateStyle: 'medium' });
}

function formatLocalDateTime(value: string, language: string): string {
  return Temporal.PlainDateTime.from(value).toLocaleString(language, {
    dateStyle: 'medium',
    timeStyle: 'short',
    hourCycle: 'h23',
  });
}

function provisioningReasonOf(reason: string, translate: (key: string) => string): string {
  const key = `${PROVISIONING_REASON_KEY_PREFIX}${constantCase(reason)}`;
  const text = translate(key);
  return text === key ? translate(UNKNOWN_REASON_KEY) : text;
}

/**
 * The interpolation values of the text of a notification. Dates and local times use the language of the app, the
 * ingest state, the severity, and the provisioning reason use their translation keys, and the estimates are minutes.
 */
export function textParamsOf(notification: RealtimeNotification, context: TextContext): Record<string, unknown> {
  const { translate, language } = context;
  switch (notification.kind) {
    case RealtimeNotificationKind.ShiftChanged: {
      const data = notification.data as ShiftChangedData;
      return { ...data, date: formatDate(data.date, language) };
    }
    case RealtimeNotificationKind.WorkOrderEstimateChanged: {
      const data = notification.data as WorkOrderEstimateChangedData;
      return {
        ...data,
        oldMinutes: minutesOfTimeSpan(data.oldEstimate),
        newMinutes: minutesOfTimeSpan(data.newEstimate),
      };
    }
    case RealtimeNotificationKind.WorkOrderAssigned:
    case RealtimeNotificationKind.WorkOrderRescheduled: {
      const data = notification.data as WorkOrderRescheduledData;
      return { ...data, plannedStartLocal: formatLocalDateTime(data.plannedStartLocal, language) };
    }
    case RealtimeNotificationKind.IngestFinished: {
      const data = notification.data as IngestFinishedData;
      return { ...data, state: translate(`${INGEST_STATE_KEY_PREFIX}${constantCase(data.state)}`) };
    }
    case RealtimeNotificationKind.MaintenanceCaseOpened: {
      const data = notification.data as MaintenanceCaseOpenedData;
      return { ...data, severity: translate(`${SEVERITY_KEY_PREFIX}${constantCase(data.severity)}`) };
    }
    case RealtimeNotificationKind.ProvisioningFailed: {
      const data = notification.data as ProvisioningFailedData;
      return { ...data, reason: provisioningReasonOf(data.reason, translate) };
    }
    default:
      return { ...notification.data };
  }
}

function openWorkOrder(workOrderId: string): NotificationJump {
  return { type: 'action', action: { type: WorkOrderActionType.Open, payload: { workOrderId } } };
}

/** Where the jump button of a notification goes, or `null` when the target is gone or has no page. */
export function jumpOf(notification: RealtimeNotification): NotificationJump | null {
  switch (notification.kind) {
    case RealtimeNotificationKind.ShiftChanged: {
      const data = notification.data as ShiftChangedData;
      return { type: 'route', commands: ['/employees', data.employeeId, 'schedule'], queryParams: { date: data.date } };
    }
    case RealtimeNotificationKind.WorkOrderAssigned:
      return openWorkOrder((notification.data as WorkOrderAssignedData).workOrderId);
    case RealtimeNotificationKind.WorkOrderEstimateChanged:
      return openWorkOrder((notification.data as WorkOrderEstimateChangedData).workOrderId);
    case RealtimeNotificationKind.WorkOrderRescheduled:
      return openWorkOrder((notification.data as WorkOrderRescheduledData).workOrderId);
    case RealtimeNotificationKind.WorkOrderBlocked:
      return openWorkOrder((notification.data as WorkOrderBlockedData).workOrderId);
    case RealtimeNotificationKind.WorkOrderDraftCreated:
      return {
        type: 'route',
        commands: ['/work-orders/drafts'],
        queryParams: { draftId: (notification.data as WorkOrderDraftCreatedData).draftId },
      };
    case RealtimeNotificationKind.ProvisioningFailed:
      return { type: 'route', commands: ['/employees'] };
    case RealtimeNotificationKind.MaintenanceCaseOpened:
      return {
        type: 'route',
        commands: ['/maintenance/cases', (notification.data as MaintenanceCaseOpenedData).caseId],
      };
    case RealtimeNotificationKind.IngestFinished:
      return {
        type: 'action',
        action: {
          type: FileActionType.IngestSummary,
          payload: { attemptId: (notification.data as IngestFinishedData).attemptId },
        },
      };
    case RealtimeNotificationKind.WorkOrderUnassigned:
    case RealtimeNotificationKind.WorkOrderReturnedToBacklog:
      return null;
  }
}

/** Whether the entry has a jump target. */
export function hasJump(entry: NotificationEntry): boolean {
  return jumpOf(entry.notification) !== null;
}

/** Adds an entry at the front and keeps the newest `MAX_NOTIFICATION_ENTRIES`. */
export function addEntry(
  entries: readonly NotificationEntry[],
  notification: RealtimeNotification,
): readonly NotificationEntry[] {
  return [{ notification, isOpened: false }, ...entries].slice(0, MAX_NOTIFICATION_ENTRIES);
}

export function hasNotification(entries: readonly NotificationEntry[], notificationId: string): boolean {
  return entries.some((entry) => entry.notification.notificationId === notificationId);
}
