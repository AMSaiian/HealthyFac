import type { MenuItem } from 'primeng/api';

import type { RowMenu } from '@shared/grid/table/row-menu';

import {
  type WorkOrderDraft,
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
export const APPLY_NO_RIGHT_KEY = 'WORK_ORDER_DRAFTS.TOOLTIPS.APPLY_NO_RIGHT';
export const DISMISS_NO_RIGHT_KEY = 'WORK_ORDER_DRAFTS.TOOLTIPS.DISMISS_NO_RIGHT';
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

/** The columns that show a text cell (`hf-cell-text`): their header has the minimum width of a text column. */
export const TEXT_COLUMNS: readonly WorkOrderDraftColumn[] = [WorkOrderDraftColumn.Title, WorkOrderDraftColumn.Machine];

export interface WorkOrderDraftMenuOptions {
  readonly rowMenu: RowMenu<WorkOrderDraft>;
  readonly labels: Readonly<Record<string, string>>;
  readonly canApply: boolean;
  readonly canDismiss: boolean;
  readonly onApply: (draft: WorkOrderDraft) => void;
  readonly onDismiss: (draft: WorkOrderDraft) => void;
}

/** Builds the row menu of a draft: both items always show, and an item without its right is disabled with the reason. */
export function workOrderDraftMenuItems(options: WorkOrderDraftMenuOptions): MenuItem[] {
  const { rowMenu, labels, canApply, canDismiss } = options;
  return [
    {
      ...rowMenu.menuItem(
        {
          labelKey: APPLY_KEY,
          tooltipKey: canApply ? APPLY_TOOLTIP_KEY : APPLY_NO_RIGHT_KEY,
          icon: 'pi pi-check',
          onSelect: options.onApply,
        },
        labels,
      ),
      disabled: !canApply,
    },
    {
      ...rowMenu.menuItem(
        {
          labelKey: DISMISS_KEY,
          tooltipKey: canDismiss ? DISMISS_TOOLTIP_KEY : DISMISS_NO_RIGHT_KEY,
          icon: 'pi pi-times',
          onSelect: options.onDismiss,
        },
        labels,
      ),
      disabled: !canDismiss,
    },
  ];
}
