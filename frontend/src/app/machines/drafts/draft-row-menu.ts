import type { MenuItem } from 'primeng/api';

import { createRowMenu } from '@shared/grid/table/row-menu';

export const DRAFT_MENU_KEYS = {
  review: 'DRAFTS.REVIEW',
  apply: 'DRAFTS.APPLY',
  dismiss: 'DRAFTS.DISMISS',
  reviewAfterMachine: 'DRAFTS.REVIEW_AFTER_MACHINE',
  awaitingTarget: 'DRAFTS.REVIEW_AWAITING_TARGET',
  reviewTooltip: 'DRAFTS.TOOLTIPS.REVIEW',
  applyTooltip: 'DRAFTS.TOOLTIPS.APPLY',
  dismissTooltip: 'DRAFTS.TOOLTIPS.DISMISS',
} as const;

export interface DraftRowMenuOptions {
  /** Translates a translation key. */
  readonly translate: (key: string) => string;
  /** True for a draft that belongs to an ingest attempt. A draft of another source has only **Dismiss**. */
  readonly isFromAttempt: boolean;
  /** True when a child draft waits for its create machine draft, so **Review** is off. */
  readonly isReviewDisabled: boolean;
  /** The translation key of the reason that **Apply** is off, or `null` when the draft applies as it is. */
  readonly applyReasonKey: string | null | undefined;
  /** True for a draft that waits for the machine check: **Apply** is off with its own reason, **Dismiss** stays on. */
  readonly isAwaitingTarget: boolean;
  /** True while an apply runs: **Apply** and **Dismiss** are off. */
  readonly isBusy: boolean;
  readonly onReview: () => void;
  readonly onApply: () => void;
  readonly onDismiss: () => void;
}

/**
 * Builds the row menu of a pending draft: **Review**, **Apply**, and **Dismiss**. Each item has a tooltip. An item that
 * cannot run is disabled, and its tooltip gives the reason. The menu holds no row: the handlers close over the draft.
 */
export function draftRowMenuItems(options: DraftRowMenuOptions): MenuItem[] {
  const rowMenu = createRowMenu<boolean>();
  rowMenu.activeRow = true;
  const labels = translatedLabels(options);
  const applyReason = options.isAwaitingTarget ? DRAFT_MENU_KEYS.awaitingTarget : (options.applyReasonKey ?? null);
  const items: MenuItem[] = [
    {
      ...rowMenu.menuItem(
        {
          labelKey: DRAFT_MENU_KEYS.review,
          tooltipKey: options.isReviewDisabled ? DRAFT_MENU_KEYS.reviewAfterMachine : DRAFT_MENU_KEYS.reviewTooltip,
          icon: 'pi pi-search',
          onSelect: options.onReview,
        },
        labels,
      ),
      disabled: options.isReviewDisabled,
    },
    {
      ...rowMenu.menuItem(
        {
          labelKey: DRAFT_MENU_KEYS.apply,
          tooltipKey: applyReason ?? DRAFT_MENU_KEYS.applyTooltip,
          icon: 'pi pi-check',
          onSelect: options.onApply,
        },
        labels,
      ),
      disabled: applyReason !== null || options.isBusy,
    },
    {
      ...rowMenu.menuItem(
        {
          labelKey: DRAFT_MENU_KEYS.dismiss,
          tooltipKey: DRAFT_MENU_KEYS.dismissTooltip,
          icon: 'pi pi-times',
          onSelect: options.onDismiss,
        },
        labels,
      ),
      disabled: options.isBusy,
    },
  ];
  return options.isFromAttempt ? items : items.slice(-1);
}

function translatedLabels(options: DraftRowMenuOptions): Record<string, string> {
  const reasonKeys =
    options.applyReasonKey === null || options.applyReasonKey === undefined ? [] : [options.applyReasonKey];
  const keys = [
    DRAFT_MENU_KEYS.review,
    DRAFT_MENU_KEYS.apply,
    DRAFT_MENU_KEYS.dismiss,
    DRAFT_MENU_KEYS.reviewAfterMachine,
    DRAFT_MENU_KEYS.awaitingTarget,
    DRAFT_MENU_KEYS.reviewTooltip,
    DRAFT_MENU_KEYS.applyTooltip,
    DRAFT_MENU_KEYS.dismissTooltip,
    ...reasonKeys,
  ];
  return Object.fromEntries(keys.map((key) => [key, options.translate(key)]));
}
