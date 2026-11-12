import { draftRowMenuItems, type DraftRowMenuOptions } from './draft-row-menu';

const NEEDS_REVIEW_KEY = 'DRAFTS.REASONS.NEEDS_REVIEW';

function options(overrides: Partial<DraftRowMenuOptions> = {}): DraftRowMenuOptions {
  return {
    translate: (key) => `t:${key}`,
    isReviewDisabled: false,
    applyReasonKey: null,
    isBusy: false,
    onReview: vi.fn(),
    onApply: vi.fn(),
    onDismiss: vi.fn(),
    ...overrides,
  };
}

describe('draftRowMenuItems', () => {
  it('draftRowMenuItems_ForAPendingDraft_ReturnsReviewApplyAndDismissEnabled', () => {
    const items = draftRowMenuItems(options());

    expect(items.map((item) => item.label)).toEqual(['t:DRAFTS.REVIEW', 't:DRAFTS.APPLY', 't:DRAFTS.DISMISS']);
    expect(items.map((item) => item.disabled)).toEqual([false, false, false]);
    expect(items.map((item) => item.tooltip)).toEqual([
      't:DRAFTS.TOOLTIPS.REVIEW',
      't:DRAFTS.TOOLTIPS.APPLY',
      't:DRAFTS.TOOLTIPS.DISMISS',
    ]);
  });

  it('draftRowMenuItems_ForADraftThatNeedsReview_DisablesApplyWithTheReason', () => {
    const items = draftRowMenuItems(options({ applyReasonKey: NEEDS_REVIEW_KEY }));

    expect(items[1].disabled).toBe(true);
    expect(items[1].tooltip).toBe(`t:${NEEDS_REVIEW_KEY}`);
    expect(items[0].disabled).toBe(false);
  });

  it('draftRowMenuItems_ForAChildOfAPendingMachineDraft_DisablesReviewWithTheReason', () => {
    const items = draftRowMenuItems(options({ isReviewDisabled: true }));

    expect(items[0].disabled).toBe(true);
    expect(items[0].tooltip).toBe('t:DRAFTS.REVIEW_AFTER_MACHINE');
  });

  it('draftRowMenuItems_WhileAnApplyRuns_DisablesApplyAndDismiss', () => {
    const items = draftRowMenuItems(options({ isBusy: true }));

    expect(items.map((item) => item.disabled)).toEqual([false, true, true]);
  });

  it('draftRowMenuItems_Commands_CallTheMatchingHandler', () => {
    const settings = options();
    const items = draftRowMenuItems(settings);

    items.forEach((item) => item.command?.({}));

    expect(settings.onReview).toHaveBeenCalledOnce();
    expect(settings.onApply).toHaveBeenCalledOnce();
    expect(settings.onDismiss).toHaveBeenCalledOnce();
  });
});
