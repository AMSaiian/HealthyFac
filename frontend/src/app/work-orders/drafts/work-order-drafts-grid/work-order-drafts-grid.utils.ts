import {
  WorkOrderDraftColumn,
  type WorkOrderDraftFilter,
  WorkOrderDraftSortColumn,
} from '@work-orders/drafts/work-order-draft';

export const COLUMN_LABEL_KEYS: Readonly<Record<WorkOrderDraftColumn, string>> = {
  [WorkOrderDraftColumn.Title]: 'WORK_ORDER_DRAFTS.COLUMNS.TITLE',
  [WorkOrderDraftColumn.Machine]: 'WORK_ORDER_DRAFTS.COLUMNS.MACHINE',
  [WorkOrderDraftColumn.Priority]: 'WORK_ORDER_DRAFTS.COLUMNS.PRIORITY',
  [WorkOrderDraftColumn.Category]: 'WORK_ORDER_DRAFTS.COLUMNS.CATEGORY',
  [WorkOrderDraftColumn.Estimate]: 'WORK_ORDER_DRAFTS.COLUMNS.ESTIMATE',
  [WorkOrderDraftColumn.Case]: 'WORK_ORDER_DRAFTS.COLUMNS.CASE',
  [WorkOrderDraftColumn.ProposedAt]: 'WORK_ORDER_DRAFTS.COLUMNS.PROPOSED_AT',
};

export const APPLY_KEY = 'WORK_ORDER_DRAFTS.APPLY';
export const APPLY_TOOLTIP_KEY = 'WORK_ORDER_DRAFTS.TOOLTIPS.APPLY';
export const DISMISS_KEY = 'WORK_ORDER_DRAFTS.DISMISS';
export const DISMISS_TOOLTIP_KEY = 'WORK_ORDER_DRAFTS.TOOLTIPS.DISMISS';
export const UNKNOWN_MACHINE_KEY = 'WORK_ORDER_DRAFTS.UNKNOWN_MACHINE';

export const SORTABLE_COLUMNS: readonly string[] = [WorkOrderDraftSortColumn.ProposedAt];

/** The grid lists the pending drafts only, so no column reads a filter. */
const NO_FILTER = (): Partial<WorkOrderDraftFilter> => ({});

export const FILTER_READERS: Readonly<Record<WorkOrderDraftColumn, (value: unknown) => Partial<WorkOrderDraftFilter>>> =
  {
    [WorkOrderDraftColumn.Title]: NO_FILTER,
    [WorkOrderDraftColumn.Machine]: NO_FILTER,
    [WorkOrderDraftColumn.Priority]: NO_FILTER,
    [WorkOrderDraftColumn.Category]: NO_FILTER,
    [WorkOrderDraftColumn.Estimate]: NO_FILTER,
    [WorkOrderDraftColumn.Case]: NO_FILTER,
    [WorkOrderDraftColumn.ProposedAt]: NO_FILTER,
  };

const NO_VALUE = (): unknown => null;

export const FILTER_WRITERS: Readonly<Record<WorkOrderDraftColumn, (filter: WorkOrderDraftFilter) => unknown>> = {
  [WorkOrderDraftColumn.Title]: NO_VALUE,
  [WorkOrderDraftColumn.Machine]: NO_VALUE,
  [WorkOrderDraftColumn.Priority]: NO_VALUE,
  [WorkOrderDraftColumn.Category]: NO_VALUE,
  [WorkOrderDraftColumn.Estimate]: NO_VALUE,
  [WorkOrderDraftColumn.Case]: NO_VALUE,
  [WorkOrderDraftColumn.ProposedAt]: NO_VALUE,
};
