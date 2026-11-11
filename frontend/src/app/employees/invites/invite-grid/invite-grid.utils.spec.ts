import { INVITE_MENU_KEYS, inviteMenuItems } from './invite-grid.utils';

describe('inviteMenuItems', () => {
  const labels = { [INVITE_MENU_KEYS.resend]: 'Resend', [INVITE_MENU_KEYS.revoke]: 'Revoke' };

  it('inviteMenuItems_ForAnOpenInvite_GivesResendThenRevokeWithTheDangerStyleOnRevoke', () => {
    const items = inviteMenuItems(labels, vi.fn(), vi.fn());

    expect(items.map((item) => item.label)).toEqual(['Resend', 'Revoke']);
    expect(items.map((item) => item.icon)).toEqual(['pi pi-send', 'pi pi-ban']);
    expect(items[0].styleClass).toBeUndefined();
    expect(items[1].styleClass).toContain('danger');
  });

  it('inviteMenuItems_WhenAnItemRuns_CallsOnlyItsOwnHandler', () => {
    const onResend = vi.fn();
    const onRevoke = vi.fn();
    const items = inviteMenuItems(labels, onResend, onRevoke);

    items[1].command?.({});

    expect(onRevoke).toHaveBeenCalledTimes(1);
    expect(onResend).not.toHaveBeenCalled();
  });
});
