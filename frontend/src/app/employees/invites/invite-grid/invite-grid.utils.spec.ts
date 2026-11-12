import { createRowMenu } from '@shared/grid/table/row-menu';
import type { InviteRow } from '@employees/employee';

import { INVITE_MENU_KEYS, inviteMenuItems } from './invite-grid.utils';

const labels = { [INVITE_MENU_KEYS.resend]: 'Resend', [INVITE_MENU_KEYS.revoke]: 'Revoke' };
const INVITE = { id: 'invite-1' } as InviteRow;

describe('inviteMenuItems', () => {
  it('inviteMenuItems_ForAnOpenInvite_GivesResendThenRevokeWithTheDangerStyleOnRevoke', () => {
    const items = inviteMenuItems(createRowMenu<InviteRow>(), labels, vi.fn(), vi.fn());

    expect(items.map((item) => item.label)).toEqual(['Resend', 'Revoke']);
    expect(items.map((item) => item.icon)).toEqual(['pi pi-send', 'pi pi-ban']);
    expect(items[0].styleClass).toBeUndefined();
    expect(items[1].styleClass).toContain('danger');
  });

  it('inviteMenuItems_WhenAnItemRuns_CallsOnlyItsOwnHandler', () => {
    const rowMenu = createRowMenu<InviteRow>();
    rowMenu.activeRow = INVITE;
    const onResend = vi.fn();
    const onRevoke = vi.fn();
    const items = inviteMenuItems(rowMenu, labels, onResend, onRevoke);

    items[1].command?.({});

    expect(onRevoke).toHaveBeenCalledWith(INVITE);
    expect(onResend).not.toHaveBeenCalled();
  });
});
