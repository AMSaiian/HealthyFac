export const REALTIME_HUB_PATH = '/api/realtime/v1/hub';

export enum RealtimeNotificationKind {
  ShiftChanged = 'shiftChanged',
  WorkOrderAssigned = 'workOrderAssigned',
  WorkOrderUnassigned = 'workOrderUnassigned',
  WorkOrderEstimateChanged = 'workOrderEstimateChanged',
  WorkOrderRescheduled = 'workOrderRescheduled',
  WorkOrderReturnedToBacklog = 'workOrderReturnedToBacklog',
  IngestFinished = 'ingestFinished',
  WorkOrderBlocked = 'workOrderBlocked',
  WorkOrderDraftCreated = 'workOrderDraftCreated',
  ProvisioningFailed = 'provisioningFailed',
  MaintenanceCaseOpened = 'maintenanceCaseOpened',
}

export enum RealtimeTopic {
  WorkOrders = 'workOrders',
  Schedule = 'schedule',
  Machines = 'machines',
  Cases = 'cases',
  DefinitionDrafts = 'definitionDrafts',
}

export interface ShiftChangedData {
  employeeId: string;
  date: string;
}

export interface WorkOrderAssignedData {
  workOrderId: string;
  title: string;
  plannedStartLocal: string;
}

export interface WorkOrderTitleData {
  title: string;
}

export interface WorkOrderEstimateChangedData {
  workOrderId: string;
  title: string;
  oldEstimate: string;
  newEstimate: string;
}

export interface WorkOrderRescheduledData {
  workOrderId: string;
  title: string;
  plannedStartLocal: string;
}

export interface WorkOrderBlockedData {
  workOrderId: string;
  title: string;
}

export interface WorkOrderDraftCreatedData {
  draftId: string;
  title: string;
}

export interface ProvisioningFailedData {
  employeeName: string;
  reason: string;
}

export interface MaintenanceCaseOpenedData {
  caseId: string;
  severity: string;
}

export interface IngestFinishedData {
  attemptId: string;
  fileName: string;
  state: string;
}

export interface RealtimeDataByKind {
  [RealtimeNotificationKind.ShiftChanged]: ShiftChangedData;
  [RealtimeNotificationKind.WorkOrderAssigned]: WorkOrderAssignedData;
  [RealtimeNotificationKind.WorkOrderUnassigned]: WorkOrderTitleData;
  [RealtimeNotificationKind.WorkOrderEstimateChanged]: WorkOrderEstimateChangedData;
  [RealtimeNotificationKind.WorkOrderRescheduled]: WorkOrderRescheduledData;
  [RealtimeNotificationKind.WorkOrderReturnedToBacklog]: WorkOrderTitleData;
  [RealtimeNotificationKind.IngestFinished]: IngestFinishedData;
  [RealtimeNotificationKind.WorkOrderBlocked]: WorkOrderBlockedData;
  [RealtimeNotificationKind.WorkOrderDraftCreated]: WorkOrderDraftCreatedData;
  [RealtimeNotificationKind.ProvisioningFailed]: ProvisioningFailedData;
  [RealtimeNotificationKind.MaintenanceCaseOpened]: MaintenanceCaseOpenedData;
}

export interface RealtimeNotification<TKind extends RealtimeNotificationKind = RealtimeNotificationKind> {
  notificationId: string;
  kind: TKind;
  occurredAt: string;
  actorSubject: string | null;
  data: RealtimeDataByKind[TKind];
}

export interface RealtimeHint {
  topic: RealtimeTopic;
  caseId?: string;
  attemptId?: string;
  machineId?: string;
  workOrderId?: string;
}
