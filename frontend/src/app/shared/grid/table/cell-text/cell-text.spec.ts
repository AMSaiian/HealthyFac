import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Tooltip } from 'primeng/tooltip';

import { CellText } from '@shared/grid/table/cell-text/cell-text';

const TWO_LINES_HEIGHT = 40;
const THREE_LINES_HEIGHT = 60;
const TEXT = 'A long text that needs more than two lines in a narrow column';

let observerCallbacks: (() => void)[] = [];

class FakeResizeObserver {
  readonly observe = vi.fn();
  readonly unobserve = vi.fn();
  readonly disconnect = vi.fn();

  constructor(callback: () => void) {
    observerCallbacks.push(callback);
  }
}

function setup(scrollHeight: number): {
  tooltip: Tooltip;
  component: CellText;
  resizeTo: (height: number) => void;
  detect: () => void;
} {
  observerCallbacks = [];
  vi.stubGlobal('ResizeObserver', FakeResizeObserver);
  let currentScrollHeight = scrollHeight;
  vi.spyOn(HTMLElement.prototype, 'scrollHeight', 'get').mockImplementation(() => currentScrollHeight);
  vi.spyOn(HTMLElement.prototype, 'clientHeight', 'get').mockReturnValue(TWO_LINES_HEIGHT);
  TestBed.configureTestingModule({});
  const fixture = TestBed.createComponent(CellText);
  fixture.componentRef.setInput('text', TEXT);
  fixture.detectChanges();
  return {
    tooltip: fixture.debugElement.query(By.directive(Tooltip)).injector.get(Tooltip),
    component: fixture.componentInstance,
    resizeTo: (height) => {
      currentScrollHeight = height;
      observerCallbacks.forEach((callback) => callback());
      fixture.detectChanges();
    },
    detect: () => fixture.detectChanges(),
  };
}

describe('CellText', { tags: ['Persistence'] }, () => {
  afterEach(() => {
    vi.restoreAllMocks();
    vi.unstubAllGlobals();
  });

  it('isCut_WithTextTallerThanTwoLines_EnablesTheTooltip', () => {
    const { tooltip, component, detect } = setup(THREE_LINES_HEIGHT);
    detect();

    expect(component.isCut()).toBe(true);
    expect(tooltip.tooltipDisabled()).toBe(false);
  });

  it('isCut_WithTextThatFits_DisablesTheTooltip', () => {
    const { tooltip, component, detect } = setup(TWO_LINES_HEIGHT);
    detect();

    expect(component.isCut()).toBe(false);
    expect(tooltip.tooltipDisabled()).toBe(true);
  });

  it('resize_WhenTheCellGetsWide_DisablesTheTooltip', () => {
    const { tooltip, resizeTo, detect } = setup(THREE_LINES_HEIGHT);
    detect();
    expect(tooltip.tooltipDisabled()).toBe(false);

    resizeTo(TWO_LINES_HEIGHT);

    expect(tooltip.tooltipDisabled()).toBe(true);
  });

  it('tooltip_Always_ShowsTheFullText', () => {
    const { tooltip } = setup(THREE_LINES_HEIGHT);

    expect(tooltip.content()).toBe(TEXT);
  });
});
