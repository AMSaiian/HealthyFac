import { Location, PlatformNavigation } from '@angular/common';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { z } from 'zod/mini';

import { provideActionHosts } from './action-registry';
import { ActionStore } from './action.store';
import type { ActionHostRegistration, ComposeCodec } from './app-action';

import { APP_ROUTER_FEATURES } from '@app/app-router';
import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';

const CREATE_TYPE = 'things.create';
const SAVED_TYPE = 'things.saved';
const REVIEW_TYPE = 'things.review';
const DRAWER_TYPE = 'things.drawer';
const DRAWER = { type: DRAWER_TYPE, payload: { id: '1' } };
const PARENT = { type: CREATE_TYPE, payload: { name: 'Lathe' } };
const CHILD = { type: REVIEW_TYPE, payload: { name: 'Review' } };

@Component({ template: '', host: { class: 'contents' } })
class Blank {}

const drawerCodec: ComposeCodec = {
  name: 'thing',
  encode: (action) => {
    const payload = action.payload as Record<string, unknown>;
    return action.type === DRAWER_TYPE && Object.keys(payload).join() === 'id'
      ? `thing:${String(payload['id'])}`
      : null;
  },
  decode: (id) => (id === null ? null : { type: DRAWER_TYPE, payload: { id } }),
};
const drawerRegistration: ActionHostRegistration = {
  type: DRAWER_TYPE,
  compose: drawerCodec,
  load: () =>
    Promise.resolve({
      component: class DrawerHost {},
      schema: z.object({ id: z.string(), note: z.optional(z.string()) }),
    }),
};

let reviewGate: Promise<void> | null = null;

const registration: ActionHostRegistration = {
  type: CREATE_TYPE,
  load: () => Promise.resolve({ component: class TestHost {}, schema: z.object({ name: z.string() }) }),
};
const reviewRegistration: ActionHostRegistration = {
  type: REVIEW_TYPE,
  load: async () => {
    await reviewGate;
    return { component: class ReviewHost {}, schema: z.object({ name: z.string() }) };
  },
};

describe('ActionStore', { tags: ['Platform'] }, () => {
  function createStore(startState?: unknown): InstanceType<typeof ActionStore> {
    TestBed.configureTestingModule({
      providers: [
        provideActionHosts([registration, reviewRegistration, drawerRegistration]),
        provideRouter(
          [
            { path: 'files', component: Blank },
            { path: 'a', component: Blank },
            { path: 'b', component: Blank },
            { path: 'c', component: Blank },
            { path: 'guarded', component: Blank, canActivate: [() => false] },
            { path: 'machines/:id/metrics', component: Blank },
            { path: 'other', component: Blank },
          ],
          ...APP_ROUTER_FEATURES,
        ),
        provideFakePlatformNavigation(),
        provideFakeAuthStore({ hasPermission: () => true }),
      ],
    });
    (TestBed.inject(PlatformNavigation) as FakePlatformNavigation).seedCurrentEntryState(startState);
    return TestBed.inject(ActionStore);
  }

  function navigationOf(): FakePlatformNavigation {
    return TestBed.inject(PlatformNavigation) as FakePlatformNavigation;
  }

  it('dispatch_WithRegisteredType_SetsActive', async () => {
    const store = createStore();
    const action = { type: CREATE_TYPE, payload: { name: 'Lathe' } };

    await store.dispatch(action);

    expect(store.active()).toEqual(action);
  });

  it.each([
    ['WithInvalidPayload', { type: CREATE_TYPE, payload: { name: 42 } }],
    ['WithUnknownType', { type: 'things.unknown', payload: {} }],
  ])('dispatch_%s_DoesNotOpen', async (_name, action) => {
    const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const store = createStore();

    await store.dispatch(action);

    expect(store.active()).toBeNull();
    expect(logged).toHaveBeenCalledTimes(1);
  });

  it('complete_Twice_IncreasesSequence', async () => {
    const store = createStore();
    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Lathe' } });

    store.complete(SAVED_TYPE);
    const first = store.lastResult();
    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Press' } });
    store.complete(SAVED_TYPE);
    const second = store.lastResult();

    expect(first).toEqual({ type: SAVED_TYPE, sequence: 1 });
    expect(second).toEqual({ type: SAVED_TYPE, sequence: 2 });
    expect(store.active()).toBeNull();
  });

  it('notify_WithOpenAction_KeepsTheActionAndRaisesTheSequence', async () => {
    const store = createStore();
    await store.dispatch(PARENT);

    store.notify(SAVED_TYPE, { id: 'a' });

    expect(store.active()).toEqual(PARENT);
    expect(store.lastResult()).toEqual({ type: SAVED_TYPE, sequence: 1, detail: { id: 'a' } });
  });

  it('notify_ThenComplete_GivesIncreasingSequences', async () => {
    const store = createStore();
    await store.dispatch(PARENT);

    store.notify(SAVED_TYPE);
    const first = store.lastResult();
    store.complete(SAVED_TYPE);
    const second = store.lastResult();

    expect(first?.sequence).toBe(1);
    expect(second?.sequence).toBe(2);
    expect(store.active()).toBeNull();
  });

  it('cancel_Always_ClearsActiveWithoutResult', async () => {
    const store = createStore();
    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Lathe' } });

    store.cancel();

    expect(store.active()).toBeNull();
    expect(store.lastResult()).toBeNull();
  });

  it('complete_WhenTheActionIsAChild_OpensTheParentAgainWithAResult', async () => {
    const store = createStore();
    await store.dispatch(PARENT);
    await store.dispatchChild(CHILD);

    store.complete(SAVED_TYPE);

    expect(store.active()).toEqual(PARENT);
    expect(store.parent()).toBeNull();
    expect(store.lastResult()).toEqual({ type: SAVED_TYPE, sequence: 1 });
  });

  it('cancel_WhenTheActionIsAChild_OpensTheParentAgain', async () => {
    const store = createStore();
    await store.dispatch(PARENT);
    await store.dispatchChild(CHILD);

    store.cancel();

    expect(store.active()).toEqual(PARENT);
    expect(store.parent()).toBeNull();
    expect(store.lastResult()).toBeNull();
  });

  it('dispatchChild_WithInvalidPayload_KeepsTheParentOpen', async () => {
    vi.spyOn(console, 'error').mockImplementation(() => undefined);
    const store = createStore();
    await store.dispatch(PARENT);

    await store.dispatchChild({ type: REVIEW_TYPE, payload: { name: 42 } });

    expect(store.active()).toEqual(PARENT);
    expect(store.parent()).toBeNull();
  });

  it('dispatch_WhileAChildIsOpen_DropsTheParent', async () => {
    const store = createStore();
    await store.dispatch(PARENT);
    await store.dispatchChild(CHILD);

    await store.dispatch(PARENT);
    store.cancel();

    expect(store.active()).toBeNull();
  });

  it('complete_WithAReturnAction_ReopensTheCallingAction', async () => {
    const store = createStore();
    const caller = { type: CREATE_TYPE, payload: { name: 'Lathe' } };
    await store.dispatch(caller);

    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Press' } }, caller);
    store.complete(SAVED_TYPE);

    expect(store.active()).toEqual(caller);
    expect(store.lastResult()?.type).toBe(SAVED_TYPE);
  });

  it('cancel_WithAReturnAction_ReopensTheCallingAction', async () => {
    const store = createStore();
    const caller = { type: CREATE_TYPE, payload: { name: 'Lathe' } };

    await store.dispatch({ type: CREATE_TYPE, payload: { name: 'Press' } }, caller);
    store.cancel();

    expect(store.active()).toEqual(caller);
  });

  describe('entry state', () => {
    beforeEach(() => {
      reviewGate = null;
    });

    it('restore_OnBackForwardAndRefresh_OpensTheDialogAgain', async () => {
      const store = createStore();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/a');
      await store.dispatch(PARENT);
      await router.navigateByUrl('/b');
      await vi.waitFor(() => expect(store.active()).toBeNull());

      TestBed.inject(Location).back();
      await vi.waitFor(() => expect(store.active()).toEqual(PARENT));
      const savedState = navigationOf().currentEntry.getState();
      TestBed.inject(Location).forward();
      await vi.waitFor(() => expect(store.active()).toBeNull());
      TestBed.resetTestingModule();
      const refreshed = createStore(savedState);
      await TestBed.inject(Router).navigateByUrl('/a');

      await vi.waitFor(() => expect(refreshed.active()).toEqual(PARENT));
    });

    it.each([
      ['UnregisteredType', { type: 'things.unknown', payload: {} }],
      ['InvalidPayload', { type: CREATE_TYPE, payload: { name: 42 } }],
    ])('restore_WithAnInvalidState_%s_OpensNothingAndLogs', async (_name, action) => {
      const logged = vi.spyOn(console, 'error').mockImplementation(() => undefined);
      const store = createStore({ active: action, parent: null });

      await TestBed.inject(Router).navigateByUrl('/a');

      await vi.waitFor(() => expect(logged).toHaveBeenCalledTimes(1));
      expect(store.active()).toBeNull();
    });

    it.each([
      ['WithoutState', undefined],
      ['WithAnotherShape', { something: 1 }],
    ])('restore_%s_LeavesNoAction', async (_name, state) => {
      const store = createStore(state);

      await TestBed.inject(Router).navigateByUrl('/a');

      expect(store.active()).toBeNull();
    });

    it('restore_AfterSkipLocationChange_LeavesNoActionAndWritesNothing', async () => {
      const store = createStore();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/a');
      await store.dispatch(PARENT);

      await router.navigate(['/b'], { skipLocationChange: true });
      await store.dispatch(CHILD);
      store.cancel();

      expect(store.active()).toBeNull();
      expect(navigationOf().currentEntry.getState()).toEqual({ active: PARENT, parent: null });
    });

    it('cancelledNavigation_KeepsActiveAndParent', async () => {
      const store = createStore();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/a');
      await store.dispatch(PARENT);
      await store.dispatchChild(CHILD);

      await router.navigateByUrl('/guarded');

      expect(store.active()).toEqual(CHILD);
      expect(store.parent()).toEqual(PARENT);
    });

    it('replaceUrlNavigation_KeepsTheDialogInTheEntryState', async () => {
      const store = createStore();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/a');
      await store.dispatch(PARENT);

      await router.navigateByUrl('/a?x=1', { replaceUrl: true });

      await vi.waitFor(() => expect(store.active()).toEqual(PARENT));
      expect(navigationOf().currentEntry.getState()).toEqual({ active: PARENT, parent: null });
    });

    it('restore_NewerNavigationDuringValidation_DropsTheOlderResult', async () => {
      const store = createStore();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/a');
      await store.dispatch(PARENT);
      await router.navigateByUrl('/b');
      await store.dispatch(CHILD);
      await router.navigateByUrl('/c');
      let release = (): void => undefined;
      reviewGate = new Promise<void>((resolve) => {
        release = resolve;
      });

      TestBed.inject(Location).back();
      await vi.waitFor(() => expect(router.url).toBe('/b'));
      TestBed.inject(Location).back();
      await vi.waitFor(() => expect(router.url).toBe('/a'));
      await vi.waitFor(() => expect(store.active()).toEqual(PARENT));
      release();
      await new Promise((resolve) => setTimeout(resolve));

      expect(store.active()).toEqual(PARENT);
    });
  });

  describe('compose', () => {
    async function openDrawer(): Promise<{
      store: InstanceType<typeof ActionStore>;
      router: Router;
      navigation: FakePlatformNavigation;
    }> {
      const store = createStore();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/a');
      await store.dispatch(DRAWER);
      await vi.waitFor(() => expect(store.active()).toEqual(DRAWER));
      return { store, router, navigation: navigationOf() };
    }

    async function loadDrawerUrl(url: string): Promise<{
      store: InstanceType<typeof ActionStore>;
      router: Router;
      navigation: FakePlatformNavigation;
    }> {
      const store = createStore();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/b');
      await router.navigateByUrl(url);
      await vi.waitFor(() => expect(store.active()).toEqual(DRAWER));
      return { store, router, navigation: navigationOf() };
    }

    it('dispatch_DrawerWithoutCompose_PushesCompose', async () => {
      const store = createStore();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/a');
      const entryCount = navigationOf().entries().length;

      await store.dispatch(DRAWER);
      await vi.waitFor(() => expect(store.active()).toEqual(DRAWER));

      expect(router.url).toBe('/a?compose=thing:1');
      expect(navigationOf().entries().length).toBe(entryCount + 1);
      expect(store.isComposed()).toBe(true);
    });

    it('dispatch_WithPrefillField_OpensInMemory', async () => {
      const store = createStore();
      const router = TestBed.inject(Router);
      await router.navigateByUrl('/a');
      const prefilled = { type: DRAWER_TYPE, payload: { id: '1', note: 'x' } };

      await store.dispatch(prefilled);

      expect(store.active()).toEqual(prefilled);
      expect(router.url).toBe('/a');
      expect(store.isComposed()).toBe(false);
    });

    it('dispatch_WhileComposeIsSet_OpensInMemory', async () => {
      const { store, router, navigation } = await openDrawer();
      const entryCount = navigation.entries().length;
      const other = { type: DRAWER_TYPE, payload: { id: '2' } };

      await store.dispatch(other);

      expect(store.active()).toEqual(other);
      expect(router.url).toBe('/a?compose=thing:1');
      expect(navigation.entries().length).toBe(entryCount);
    });

    it('cancel_ComposedDrawer_GoesBackOneEntry', async () => {
      const { store, router, navigation } = await openDrawer();

      store.cancel();
      await vi.waitFor(() => expect(router.url).toBe('/a'));

      expect(store.active()).toBeNull();
      expect(store.isComposed()).toBe(false);
      expect(navigation.currentEntry.index).toBe(navigation.entries().length - 2);
    });

    it('cancel_ComposedDrawerFromALoadedUrl_ReplacesTheUrl', async () => {
      const { store, router, navigation } = await loadDrawerUrl('/a?compose=thing:1');
      const entryCount = navigation.entries().length;

      store.cancel();
      await vi.waitFor(() => expect(router.url).toBe('/a'));

      expect(navigation.entries().length).toBe(entryCount);
      expect(store.active()).toBeNull();
    });

    it('complete_ChildOfComposedDrawer_ReturnsToTheDrawerAndKeepsCompose', async () => {
      const { store, router } = await openDrawer();
      await store.dispatchChild(CHILD);

      store.complete(SAVED_TYPE);

      expect(store.active()).toEqual(DRAWER);
      expect(store.isComposed()).toBe(true);
      expect(router.url).toBe('/a?compose=thing:1');
    });

    it('restore_InvalidComposeId_RemovesCompose', async () => {
      const store = createStore();
      const router = TestBed.inject(Router);

      await router.navigateByUrl('/a?compose=thing');

      await vi.waitFor(() => expect(router.url).toBe('/a'));
      expect(store.active()).toBeNull();
    });

    it('restore_BrowserBackFromDrawerEntry_ClosesTheDrawer', async () => {
      const { store, router } = await openDrawer();

      TestBed.inject(Location).back();

      await vi.waitFor(() => expect(router.url).toBe('/a'));
      expect(store.active()).toBeNull();
      expect(store.isComposed()).toBe(false);
    });

    it('restore_BrowserBackWithChild_ClosesBoth', async () => {
      const { store, router } = await openDrawer();
      await store.dispatchChild(CHILD);

      TestBed.inject(Location).back();

      await vi.waitFor(() => expect(router.url).toBe('/a'));
      expect(store.active()).toBeNull();
      expect(store.parent()).toBeNull();
    });

    it('restore_RefreshWithChild_ReopensOnlyTheDrawer', async () => {
      const { store, navigation } = await openDrawer();
      await store.dispatchChild(CHILD);
      const savedState = navigation.currentEntry.getState();
      TestBed.resetTestingModule();
      const refreshed = createStore(savedState);

      await TestBed.inject(Router).navigateByUrl('/a?compose=thing:1');

      await vi.waitFor(() => expect(refreshed.active()).toEqual(DRAWER));
      expect(refreshed.parent()).toBeNull();
    });

    it('cancel_After404Load_RemovesCompose', async () => {
      const { store, router } = await loadDrawerUrl('/a?compose=thing:1');

      store.cancel();

      await vi.waitFor(() => expect(router.url).toBe('/a'));
    });
  });
});
