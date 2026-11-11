import type { MenuItem } from 'primeng/api';

export const INVITE_MENU_KEYS = {
  resend: 'EMPLOYEES.RESEND',
  revoke: 'EMPLOYEES.REVOKE',
} as const;

const REVOKE_STYLE_CLASS = 'hf-menu-danger';

/** Builds the row menu of an open invite: resend it, or revoke it. The revoke item has the danger style. */
export function inviteMenuItems(
  labels: Readonly<Record<string, string>>,
  onResend: () => void,
  onRevoke: () => void,
): MenuItem[] {
  return [
    {
      label: labels[INVITE_MENU_KEYS.resend] ?? INVITE_MENU_KEYS.resend,
      icon: 'pi pi-send',
      command: onResend,
    },
    {
      label: labels[INVITE_MENU_KEYS.revoke] ?? INVITE_MENU_KEYS.revoke,
      icon: 'pi pi-ban',
      styleClass: REVOKE_STYLE_CLASS,
      command: onRevoke,
    },
  ];
}
