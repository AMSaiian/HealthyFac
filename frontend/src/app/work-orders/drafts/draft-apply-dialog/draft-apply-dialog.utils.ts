import type { ApplyWorkOrderDraftBody, WorkOrderDraft } from '@work-orders/drafts/work-order-draft';
import type { WorkOrderFormValue } from '@work-orders/work-order-schema';

/**
 * The form value of a draft. The apply does not take the machine or the files: the work order gets the machine of
 * the draft, and no file.
 */
export function toFormValue(draft: WorkOrderDraft): WorkOrderFormValue {
  return {
    title: draft.title,
    description: draft.description ?? '',
    priority: draft.priority,
    category: draft.category,
    machineId: draft.machineId,
    estimateMinutes: draft.estimateMinutes,
    fileIds: [],
  };
}

/** The apply body: the edited values and the version of the draft that the user read. An empty text is `null`. */
export function toApplyBody(draft: WorkOrderDraft, value: WorkOrderFormValue): ApplyWorkOrderDraftBody {
  const description = (value.description ?? '').trim();
  return {
    rowVersion: draft.rowVersion,
    title: value.title.trim(),
    description: description === '' ? null : description,
    priority: value.priority,
    category: value.category,
    estimateMinutes: value.estimateMinutes,
  };
}
