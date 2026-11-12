import { DraftKind, type MachineDraftGridItem } from '@machines/drafts/draft';
import type { DraftRef } from '@machines/drafts/with-draft-quick-apply';

/** The order of an apply of checked drafts: a machine draft first, because its children need the machine. */
const APPLY_ORDER: Readonly<Record<DraftKind, number>> = {
  [DraftKind.Machine]: 0,
  [DraftKind.Metric]: 1,
  [DraftKind.Status]: 2,
};

/** Checks or unchecks the given drafts. The input stays as it is. */
export function withSelected(
  selection: Readonly<Record<string, MachineDraftGridItem>>,
  items: readonly MachineDraftGridItem[],
  isChecked: boolean,
): Readonly<Record<string, MachineDraftGridItem>> {
  const next = { ...selection };
  for (const item of items) {
    if (isChecked) {
      next[item.id] = item;
    } else {
      delete next[item.id];
    }
  }
  return next;
}

/** Lists the checked drafts in the order of the apply: machine drafts, then metric drafts, then status drafts. */
export function toApplyList(items: readonly MachineDraftGridItem[]): DraftRef[] {
  return [...items]
    .sort((a, b) => APPLY_ORDER[a.kind] - APPLY_ORDER[b.kind])
    .map((item) => ({ attemptId: item.attemptId, draftId: item.id }));
}
