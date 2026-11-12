import { hasOpenPanel, isDialogTopmost } from './action-dialog.utils';

const CONTAINER_Z_INDEX = 1100;

function dialogAt(zIndex: number): HTMLElement {
  const dialog = document.createElement('div');
  dialog.className = 'p-dialog';
  dialog.style.zIndex = String(zIndex);
  document.body.appendChild(dialog);
  return dialog;
}

afterEach(() => {
  document.body.replaceChildren();
});

describe('isDialogTopmost', () => {
  it('isDialogTopmost_WithAConfirmationAbove_ReturnsFalse', () => {
    const container = dialogAt(CONTAINER_Z_INDEX);
    dialogAt(CONTAINER_Z_INDEX + 1);

    const isTop = isDialogTopmost(container, document);

    expect(isTop).toBe(false);
  });

  it('isDialogTopmost_ForTheConfirmationAboveTheContainer_ReturnsTrue', () => {
    dialogAt(CONTAINER_Z_INDEX);
    const confirmation = dialogAt(CONTAINER_Z_INDEX + 1);

    const isTop = isDialogTopmost(confirmation, document);

    expect(isTop).toBe(true);
  });

  it('isDialogTopmost_WithOnlyTheContainer_ReturnsTrue', () => {
    const container = dialogAt(CONTAINER_Z_INDEX);

    const isTop = isDialogTopmost(container, document);

    expect(isTop).toBe(true);
  });
});

describe('hasOpenPanel', () => {
  it('hasOpenPanel_WithoutAnOverlay_ReturnsFalse', () => {
    const hasPanel = hasOpenPanel(document);

    expect(hasPanel).toBe(false);
  });

  it('hasOpenPanel_WithAFilterOverlay_ReturnsTrue', () => {
    const overlay = document.createElement('div');
    overlay.className = 'p-datatable-filter-overlay';
    document.body.appendChild(overlay);

    const hasPanel = hasOpenPanel(document);

    expect(hasPanel).toBe(true);
  });
});
