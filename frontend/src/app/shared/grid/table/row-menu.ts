import type { MenuItem } from 'primeng/api';
import type { Menu } from 'primeng/menu';

const DANGER_STYLE_CLASS = 'hf-menu-danger';

/** The label keys of the destructive items (Delete, Revoke, Dismiss). Every menu shows them in the danger style. */
const DANGER_LABEL_KEYS: readonly string[] = ['ACTIONS.DELETE', 'EMPLOYEES.REVOKE', 'DRAFTS.DISMISS'];

export interface RowMenuItem<TRow> {
  readonly labelKey: string;
  readonly tooltipKey: string;
  readonly icon: string;
  readonly onSelect: (row: TRow) => void;
}

/** Keeps the row of the open row menu, so each item acts on that row. */
export class RowMenu<TRow> {
  activeRow: TRow | null = null;

  open(event: Event, row: TRow, menu: Menu): void {
    this.activeRow = row;
    menu.toggle(event);
  }

  menuItem(item: RowMenuItem<TRow>, labels: Readonly<Record<string, string>>): MenuItem {
    return {
      label: labels[item.labelKey] ?? item.labelKey,
      tooltip: labels[item.tooltipKey] ?? item.tooltipKey,
      icon: item.icon,
      styleClass: DANGER_LABEL_KEYS.includes(item.labelKey) ? DANGER_STYLE_CLASS : undefined,
      command: (): void => {
        const row = this.activeRow;
        if (row !== null) {
          item.onSelect(row);
        }
      },
    };
  }
}

export function createRowMenu<TRow>(): RowMenu<TRow> {
  return new RowMenu<TRow>();
}
