import type { MenuItem } from 'primeng/api';

export const DRAFT_MENU_KEYS = {
  review: 'DRAFTS.REVIEW',
  apply: 'DRAFTS.APPLY',
  dismiss: 'DRAFTS.DISMISS',
  reviewAfterMachine: 'DRAFTS.REVIEW_AFTER_MACHINE',
} as const;

export interface DraftRowMenuOptions {
  /** Translates a translation key. */
  readonly translate: (key: string) => string;
  /** True when a child draft waits for its create machine draft, so **Review** is off. */
  readonly isReviewDisabled: boolean;
  /** The translation key of the reason that **Apply** is off, or `null` when the draft applies as it is. */
  readonly applyReasonKey: string | null | undefined;
  /** True while an apply runs: **Apply** and **Dismiss** are off. */
  readonly isBusy: boolean;
  readonly onReview: () => void;
  readonly onApply: () => void;
  readonly onDismiss: () => void;
}

/**
 * Builds the row menu of a pending draft: **Review**, **Apply**, and **Dismiss**. An item that cannot run is
 * disabled and shows its reason as the item tooltip.
 */
export function draftRowMenuItems(options: DraftRowMenuOptions): MenuItem[] {
  const { translate } = options;
  const applyReason = options.applyReasonKey ?? null;
  return [
    {
      label: translate(DRAFT_MENU_KEYS.review),
      icon: 'pi pi-search',
      disabled: options.isReviewDisabled,
      ...(options.isReviewDisabled
        ? { tooltipOptions: { tooltipLabel: translate(DRAFT_MENU_KEYS.reviewAfterMachine) } }
        : {}),
      command: options.onReview,
    },
    {
      label: translate(DRAFT_MENU_KEYS.apply),
      icon: 'pi pi-check',
      disabled: applyReason !== null || options.isBusy,
      ...(applyReason === null ? {} : { tooltipOptions: { tooltipLabel: translate(applyReason) } }),
      command: options.onApply,
    },
    {
      label: translate(DRAFT_MENU_KEYS.dismiss),
      icon: 'pi pi-times',
      disabled: options.isBusy,
      command: options.onDismiss,
    },
  ];
}
