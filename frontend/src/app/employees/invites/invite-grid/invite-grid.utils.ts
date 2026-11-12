import type { MenuItem } from 'primeng/api';

import type { InviteRow } from '@employees/employee';
import type { RowMenu } from '@shared/grid/table/row-menu';

export const INVITE_MENU_KEYS = {
  resend: 'EMPLOYEES.RESEND',
  revoke: 'EMPLOYEES.REVOKE',
  resendTooltip: 'EMPLOYEES.TOOLTIPS.RESEND',
  revokeTooltip: 'EMPLOYEES.TOOLTIPS.REVOKE',
} as const;

/** Builds the row menu of an open invite: resend it, or revoke it. The revoke item has the danger style. */
export function inviteMenuItems(
  rowMenu: RowMenu<InviteRow>,
  labels: Readonly<Record<string, string>>,
  onResend: (row: InviteRow) => void,
  onRevoke: (row: InviteRow) => void,
): MenuItem[] {
  return [
    rowMenu.menuItem(
      {
        labelKey: INVITE_MENU_KEYS.resend,
        tooltipKey: INVITE_MENU_KEYS.resendTooltip,
        icon: 'pi pi-send',
        onSelect: onResend,
      },
      labels,
    ),
    rowMenu.menuItem(
      {
        labelKey: INVITE_MENU_KEYS.revoke,
        tooltipKey: INVITE_MENU_KEYS.revokeTooltip,
        icon: 'pi pi-ban',
        onSelect: onRevoke,
      },
      labels,
    ),
  ];
}
