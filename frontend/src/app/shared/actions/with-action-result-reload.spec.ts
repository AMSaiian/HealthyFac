import { signal, type WritableSignal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { signalStore, withProps } from '@ngrx/signals';
import { vi } from 'vitest';

import type { ActionResult } from './app-action';
import { withActionResultReload } from './with-action-result-reload';

const RELOAD_TYPE = 'things.saved';
const OTHER_TYPE = 'things.opened';

function startStore(lastResult: WritableSignal<ActionResult | null>, reload: () => void): void {
  const Store = signalStore(
    withProps(() => ({ _actions: { lastResult: lastResult.asReadonly() } })),
    withActionResultReload([RELOAD_TYPE], reload),
  );
  TestBed.runInInjectionContext(() => new Store());
}

describe('withActionResultReload', () => {
  it.each([
    { name: 'an older sequence', result: { type: RELOAD_TYPE, sequence: 2 } },
    { name: 'another type', result: { type: OTHER_TYPE, sequence: 4 } },
  ])('withActionResultReload_WithOlderOrOtherResult_DoesNotReload: $name', ({ result }) => {
    const lastResult = signal<ActionResult | null>({ type: RELOAD_TYPE, sequence: 3 });
    const reload = vi.fn();
    startStore(lastResult, reload);
    TestBed.tick();

    lastResult.set(result);
    TestBed.tick();

    expect(reload).not.toHaveBeenCalled();
  });

  it('withActionResultReload_WithNewerListedResult_ReloadsOnce', () => {
    const lastResult = signal<ActionResult | null>(null);
    const reload = vi.fn();
    startStore(lastResult, reload);
    TestBed.tick();

    lastResult.set({ type: RELOAD_TYPE, sequence: 1 });
    TestBed.tick();

    expect(reload).toHaveBeenCalledTimes(1);
  });
});
