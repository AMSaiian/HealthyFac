import { FILE_EDIT_COMPOSE, FileActionType } from '@files/file-actions';
import { DraftActionType } from '@machines/drafts/draft-actions';
import { MACHINE_COMPOSE, MACHINE_DRAFTS_COMPOSE, MachineActionType } from '@machines/machine-actions';
import { WORK_ORDER_COMPOSE, WorkOrderActionType } from '@work-orders/work-order-actions';
import { EmployeeActionType } from '@employees/employee-actions';
import type { ActionHostRegistration } from '@shared/actions/app-action';

/** The action hosts of the app. Route flow tests register the same list. */
export const APP_ACTION_HOSTS: readonly ActionHostRegistration[] = [
  ...[MachineActionType.Create, MachineActionType.Update].map((type) => ({
    type,
    compose: MACHINE_COMPOSE,
    load: async () => {
      const [host, form] = await Promise.all([
        import('@machines/drawer/machine-drawer-host/machine-drawer-host'),
        import('@machines/drawer/machine-form/machine-form-schema'),
      ]);
      return { component: host.MachineDrawerHost, schema: form.machineActionPayloadSchema };
    },
  })),
  {
    type: WorkOrderActionType.Open,
    compose: WORK_ORDER_COMPOSE,
    load: async () => {
      const [host, schema] = await Promise.all([
        import('@work-orders/drawer/work-order-drawer-host/work-order-drawer-host'),
        import('@work-orders/work-order-schema'),
      ]);
      return { component: host.WorkOrderDrawerHost, schema: schema.workOrderActionPayloadSchema };
    },
  },
  ...[
    WorkOrderActionType.Assign,
    WorkOrderActionType.Move,
    WorkOrderActionType.Unassign,
    WorkOrderActionType.ChangeStatus,
    WorkOrderActionType.Delete,
  ].map((type) => ({
    type,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@work-orders/schedule-actions/schedule-actions-host/schedule-actions-host'),
        import('@work-orders/work-order-actions'),
      ]);
      return { component: host.ScheduleActionsHost, schema: payloads.workOrderRowActionPayloadSchema };
    },
  })),
  ...[
    EmployeeActionType.Invite,
    EmployeeActionType.Resend,
    EmployeeActionType.RetryProvisioning,
    EmployeeActionType.Revoke,
  ].map((type) => ({
    type,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@employees/employee-actions-host/employee-actions-host'),
        import('@employees/employee-action-payloads'),
      ]);
      return { component: host.EmployeeActionsHost, schema: payloads.employeeActionPayloadSchema };
    },
  })),
  {
    type: FileActionType.Edit,
    compose: FILE_EDIT_COMPOSE,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@files/edit/file-edit-host/file-edit-host'),
        import('@files/file-action-payloads'),
      ]);
      return { component: host.FileEditHost, schema: payloads.fileEditActionPayloadSchema };
    },
  },
  {
    type: MachineActionType.Drafts,
    compose: MACHINE_DRAFTS_COMPOSE,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@machines/drafts/list/machine-drafts-host/machine-drafts-host'),
        import('@machines/drafts/draft-action-payloads'),
      ]);
      return { component: host.MachineDraftsHost, schema: payloads.machineDraftsActionPayloadSchema };
    },
  },
  {
    type: MachineActionType.ChangeState,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@machines/state/machine-state-host/machine-state-host'),
        import('@machines/state/machine-state-action-payloads'),
      ]);
      return { component: host.MachineStateHost, schema: payloads.machineStateActionPayloadSchema };
    },
  },
  {
    type: DraftActionType.CompareMachine,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@machines/drafts/compare/draft-compare-host/draft-compare-host'),
        import('@machines/drafts/draft-action-payloads'),
      ]);
      return { component: host.DraftCompareHost, schema: payloads.draftCompareActionPayloadSchema };
    },
  },
  {
    type: DraftActionType.ReviewStatus,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@machines/drafts/status/status-draft-review-host/status-draft-review-host'),
        import('@machines/drafts/draft-action-payloads'),
      ]);
      return { component: host.StatusDraftReviewHost, schema: payloads.statusDraftReviewActionPayloadSchema };
    },
  },
  {
    type: DraftActionType.ReviewPhase,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@machines/drafts/phase/phase-draft-review-host/phase-draft-review-host'),
        import('@machines/drafts/draft-action-payloads'),
      ]);
      return { component: host.PhaseDraftReviewHost, schema: payloads.phaseDraftReviewActionPayloadSchema };
    },
  },
  {
    type: FileActionType.Tie,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@files/tie/file-tie-host/file-tie-host'),
        import('@files/file-action-payloads'),
      ]);
      return { component: host.FileTieHost, schema: payloads.fileTieActionPayloadSchema };
    },
  },
  {
    type: FileActionType.IngestHistory,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@files/ingest/history/ingest-history-host/ingest-history-host'),
        import('@files/file-action-payloads'),
      ]);
      return { component: host.IngestHistoryHost, schema: payloads.ingestHistoryActionPayloadSchema };
    },
  },
  {
    type: FileActionType.IngestSummary,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@files/ingest/summary/ingest-summary-host/ingest-summary-host'),
        import('@files/file-action-payloads'),
      ]);
      return { component: host.IngestSummaryHost, schema: payloads.ingestSummaryActionPayloadSchema };
    },
  },
  {
    type: FileActionType.Upload,
    load: async () => {
      const [host, payloads] = await Promise.all([
        import('@files/upload/file-upload-host/file-upload-host'),
        import('@files/file-action-payloads'),
      ]);
      return { component: host.FileUploadHost, schema: payloads.fileUploadActionPayloadSchema };
    },
  },
];
