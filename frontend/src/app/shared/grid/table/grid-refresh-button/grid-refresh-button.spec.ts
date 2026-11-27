import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import { GridRefreshButton } from './grid-refresh-button';

interface GridRefreshButtonInternals {
  requestRefresh(): void;
  isCoolingDown(): boolean;
}

describe('GridRefreshButton', { tags: ['Platform'] }, () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
  });

  function create(secondsLeft: number): { component: GridRefreshButtonInternals; emitted: ReturnType<typeof vi.fn> } {
    const fixture = TestBed.createComponent(GridRefreshButton);
    fixture.componentRef.setInput('secondsLeft', secondsLeft);
    const emitted = vi.fn();
    fixture.componentInstance.refreshRequested.subscribe(emitted);
    fixture.detectChanges();
    return { component: fixture.componentInstance as unknown as GridRefreshButtonInternals, emitted };
  }

  it('requestRefresh_WithZeroSecondsLeft_EmitsOnce', () => {
    const { component, emitted } = create(0);

    component.requestRefresh();

    expect(emitted).toHaveBeenCalledTimes(1);
  });

  it('requestRefresh_DuringTheCooldown_EmitsNothing', () => {
    const { component, emitted } = create(3);

    component.requestRefresh();

    expect(emitted).not.toHaveBeenCalled();
  });

  it('isCoolingDown_WithSecondsLeft_IsTrueAndWithZeroIsFalse', () => {
    const waiting = create(2).component.isCoolingDown();
    const idle = TestBed.createComponent(GridRefreshButton).componentInstance as unknown as GridRefreshButtonInternals;

    expect(waiting).toBe(true);
    expect(idle.isCoolingDown()).toBe(false);
  });
});
