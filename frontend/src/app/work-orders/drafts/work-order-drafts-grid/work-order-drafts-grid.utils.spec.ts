import type { MenuItem } from 'primeng/api';

import { createRowMenu } from '@shared/grid/table/row-menu';

import type { WorkOrderDraft } from '@work-orders/drafts/work-order-draft';
import {
  APPLY_KEY,
  APPLY_NO_RIGHT_KEY,
  APPLY_TOOLTIP_KEY,
  DISMISS_KEY,
  DISMISS_NO_RIGHT_KEY,
  DISMISS_TOOLTIP_KEY,
  workOrderDraftMenuItems,
} from './work-order-drafts-grid.utils';

const LABELS: Readonly<Record<string, string>> = {
  [APPLY_KEY]: 'Create work order',
  [DISMISS_KEY]: 'Dismiss draft',
  [APPLY_TOOLTIP_KEY]: 'apply tooltip',
  [DISMISS_TOOLTIP_KEY]: 'dismiss tooltip',
  [APPLY_NO_RIGHT_KEY]: 'apply reason',
  [DISMISS_NO_RIGHT_KEY]: 'dismiss reason',
};

function itemsOf(canApply: boolean, canDismiss: boolean): MenuItem[] {
  return workOrderDraftMenuItems({
    rowMenu: createRowMenu<WorkOrderDraft>(),
    labels: LABELS,
    canApply,
    canDismiss,
    onApply: () => undefined,
    onDismiss: () => undefined,
  });
}

describe('workOrderDraftMenuItems', { tags: ['WorkOrders', 'Drafts'] }, () => {
  it('menuItems_WithBothRights_EnablesBothItems', () => {
    const [apply, dismiss] = itemsOf(true, true);

    expect(apply.label).toBe('Create work order');
    expect(apply.disabled).toBe(false);
    expect(apply.tooltip).toBe('apply tooltip');
    expect(dismiss.label).toBe('Dismiss draft');
    expect(dismiss.disabled).toBe(false);
    expect(dismiss.tooltip).toBe('dismiss tooltip');
  });

  it('menuItems_DismissItem_HasTheDangerStyleAndApplyHasNone', () => {
    const [apply, dismiss] = itemsOf(true, true);

    expect(dismiss.styleClass).toBe('hf-menu-danger');
    expect(apply.styleClass).toBeUndefined();
  });

  it('menuItems_WithoutApplyRight_DisablesApplyWithItsReason', () => {
    const [apply, dismiss] = itemsOf(false, true);

    expect(apply.disabled).toBe(true);
    expect(apply.tooltip).toBe('apply reason');
    expect(dismiss.disabled).toBe(false);
  });

  it('menuItems_WithoutDismissRight_DisablesDismissWithItsReason', () => {
    const [apply, dismiss] = itemsOf(true, false);

    expect(apply.disabled).toBe(false);
    expect(dismiss.disabled).toBe(true);
    expect(dismiss.tooltip).toBe('dismiss reason');
  });

  it('menuItems_WithoutRights_ShowsBothItemsDisabled', () => {
    const items = itemsOf(false, false);

    expect(items.map((item) => item.label)).toEqual(['Create work order', 'Dismiss draft']);
    expect(items.map((item) => item.disabled)).toEqual([true, true]);
  });
});
