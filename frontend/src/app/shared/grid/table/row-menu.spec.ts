import { vi } from 'vitest';

import { createRowMenu } from './row-menu';

interface Row {
  readonly id: string;
}

const OPEN_ITEM = {
  labelKey: 'ACTIONS.OPEN',
  tooltipKey: 'ACTIONS.TOOLTIPS.OPEN',
  icon: 'pi pi-external-link',
};

describe('createRowMenu', () => {
  it('menuItem_WithoutActiveRow_DoesNotCallOnSelect', () => {
    const onSelect = vi.fn();
    const menu = createRowMenu<Row>();
    const item = menu.menuItem({ ...OPEN_ITEM, onSelect }, {});

    item.command?.({} as never);

    expect(onSelect).not.toHaveBeenCalled();
  });

  it('menuItem_WithActiveRow_CallsOnSelect', () => {
    const onSelect = vi.fn();
    const menu = createRowMenu<Row>();
    const item = menu.menuItem({ ...OPEN_ITEM, onSelect }, {});
    const row = { id: 'machine-1' };
    menu.activeRow = row;

    item.command?.({} as never);

    expect(onSelect).toHaveBeenCalledWith(row);
  });

  it.each(['ACTIONS.DELETE', 'EMPLOYEES.REVOKE', 'DRAFTS.DISMISS'])(
    'menuItem_WithADangerLabelKey_SetsTheDangerStyle_%s',
    (labelKey) => {
      const menu = createRowMenu<Row>();

      const item = menu.menuItem({ ...OPEN_ITEM, labelKey, onSelect: vi.fn() }, {});

      expect(item.styleClass).toBe('hf-menu-danger');
    },
  );

  it('menuItem_WithAnotherKey_SetsNoStyle', () => {
    const menu = createRowMenu<Row>();

    const item = menu.menuItem({ ...OPEN_ITEM, onSelect: vi.fn() }, {});

    expect(item.styleClass).toBeUndefined();
  });
});
