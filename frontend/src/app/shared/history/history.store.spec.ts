import { PlatformNavigation } from '@angular/common';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationStart, provideRouter, Router } from '@angular/router';

import type { FakePlatformNavigation } from './fake-platform-navigation';
import { provideFakePlatformNavigation } from './fake-platform-navigation';
import { HistoryStore } from './history.store';
import type { ReturnTarget } from './return-target';

import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { Permission } from '@shared/security/permissions';

@Component({ template: '', host: { class: 'contents' } })
class Blank {}

const TARGET: ReturnTarget = { url: '/target' };
const GUARDED_TARGET: ReturnTarget = { url: '/target', permission: Permission.EmployeesRead };

interface Created {
  store: InstanceType<typeof HistoryStore>;
  router: Router;
  navigation: FakePlatformNavigation;
}

describe('HistoryStore', () => {
  let hasPermission = false;

  beforeEach(() => {
    hasPermission = false;
  });

  async function create(
    options: { isNavigationMissing?: boolean; startState?: unknown; isNavigationSkipped?: boolean } = {},
  ): Promise<Created> {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: '', component: Blank },
          { path: 'a', component: Blank },
          { path: 'b', component: Blank },
          { path: 'target', component: Blank },
          { path: 'not-found', component: Blank },
        ]),
        provideFakePlatformNavigation(),
        provideFakeAuthStore({ hasPermission: () => hasPermission }),
        ...(options.isNavigationMissing === true ? [{ provide: PlatformNavigation, useValue: undefined }] : []),
      ],
    });
    const navigation = TestBed.inject(PlatformNavigation) as FakePlatformNavigation;
    navigation?.seedCurrentEntryState(options.startState);
    const store = TestBed.inject(HistoryStore);
    const router = TestBed.inject(Router);
    if (options.isNavigationSkipped === true) {
      return { store, router, navigation };
    }
    router.initialNavigation();
    await router.navigateByUrl('/');
    return { store, router, navigation };
  }

  it('back_WithPreviousEntry_CallsLocationBack', async () => {
    const { store, router, navigation } = await create();
    await router.navigateByUrl('/a');
    await router.navigateByUrl('/b');
    const entryCount = navigation.entries().length;

    await store.back(TARGET);

    expect(router.url).toBe('/a');
    expect(navigation.entries().length).toBe(entryCount);
    expect(navigation.currentEntry.index).toBe(1);
  });

  it('back_WithoutPreviousEntry_NavigatesToTheTargetUrl', async () => {
    const { store, router } = await create();
    await router.navigateByUrl('/a');

    await store.back(TARGET);
    await store.back(TARGET);

    expect(router.url).toBe('/target');
  });

  it.each([
    ['WithoutPermission', TARGET, false, true],
    ['WithPermissionHeld', GUARDED_TARGET, true, true],
    ['WithPermissionMissing', GUARDED_TARGET, false, false],
  ])('canReturn_WithoutPreviousEntry_%s', async (_name, target, isHeld, expected) => {
    hasPermission = isHeld;
    const { store } = await create();

    const result = store.canReturn(target);

    expect(result).toBe(expected);
  });

  it('canReturn_WithPreviousEntry_IsTrue', async () => {
    const { store, router } = await create();
    await router.navigateByUrl('/a');

    const result = store.canReturn(GUARDED_TARGET);

    expect(result).toBe(true);
  });

  it('takeStartState_AfterTheFirstCall_ReturnsUndefined', async () => {
    const { store } = await create({ startState: { marker: 1 }, isNavigationSkipped: true });

    const first = store.takeStartState();
    const second = store.takeStartState();

    expect(first).toEqual({ marker: 1 });
    expect(second).toBeUndefined();
  });

  it('writeEntryState_ThenReadEntryState_ReturnsTheState', async () => {
    const { store } = await create();

    store.writeEntryState({ marker: 2 });

    expect(store.readEntryState()).toEqual({ marker: 2 });
  });

  it('writeEntryState_WithoutNavigationApi_DoesNothing', async () => {
    const { store } = await create({ isNavigationMissing: true });

    const act = (): void => store.writeEntryState({ marker: 3 });

    expect(act).not.toThrow();
    expect(store.readEntryState()).toBeUndefined();
  });

  it('back_WithoutNavigationApi_NavigatesToTheTargetUrl', async () => {
    const { store, router } = await create({ isNavigationMissing: true });
    await router.navigateByUrl('/a');
    await router.navigateByUrl('/b');

    await store.back(TARGET);

    expect(router.url).toBe('/target');
  });

  it('startStateOf_AfterBrowserBack_ReturnsTheStateThatTheRouterReplaced', async () => {
    const { store, router } = await create();
    await router.navigateByUrl('/a');
    store.writeEntryState({ marker: 4 });
    await router.navigateByUrl('/b');
    let startId = -1;
    router.events.subscribe((event) => {
      if (event instanceof NavigationStart) {
        startId = event.id;
      }
    });

    await store.back(TARGET);

    expect(store.startStateOf(startId)).toEqual({ marker: 4 });
    expect(store.readEntryState()).toBeUndefined();
  });

  it('back_CalledTwiceBeforeTheNavigationEnds_GoesBackOneEntry', async () => {
    const { store, router, navigation } = await create();
    await router.navigateByUrl('/a');
    await router.navigateByUrl('/b');

    await Promise.all([store.back(TARGET), store.back(TARGET)]);

    expect(router.url).toBe('/a');
    expect(navigation.currentEntry.index).toBe(1);
  });

  describe('compose', () => {
    it('closeCompose_WithoutCompose_DoesNothing', async () => {
      const { store, router, navigation } = await create();
      await router.navigateByUrl('/a');
      const navigate = vi.spyOn(router, 'navigateByUrl');
      const entryCount = navigation.entries().length;

      const result = await store.closeCompose();

      expect(result).toBe(false);
      expect(navigate).not.toHaveBeenCalled();
      expect(navigation.entries().length).toBe(entryCount);
      expect(navigation.currentEntry.index).toBe(entryCount - 1);
    });

    it('closeCompose_CalledTwice_GoesBackOneEntry', async () => {
      const { store, router, navigation } = await create();
      store.registerComposeNames(['machine']);
      await router.navigateByUrl('/a');
      await store.openCompose('machine:m1');
      const navigate = vi.spyOn(router, 'navigateByUrl');

      await Promise.all([store.closeCompose(), store.closeCompose()]);

      expect(navigate).not.toHaveBeenCalled();
      expect(router.url).toBe('/a');
      expect(navigation.currentEntry.index).toBe(1);
    });

    it('openCompose_PushesOneEntryAndKeepsOtherParams', async () => {
      const { store, router, navigation } = await create();
      await router.navigateByUrl('/a?x=1');
      const entryCount = navigation.entries().length;

      await store.openCompose('machine:m1');

      expect(router.url).toBe('/a?x=1&compose=machine:m1');
      expect(store.compose()).toBe('machine:m1');
      expect(navigation.entries().length).toBe(entryCount + 1);
    });

    it('closeCompose_AfterAnAppPush_GoesBackOneEntry', async () => {
      const { store, router, navigation } = await create();
      store.registerComposeNames(['machine']);
      await router.navigateByUrl('/a?x=1#top');
      await store.openCompose('machine:m1');

      await store.closeCompose();

      expect(router.url).toBe('/a?x=1#top');
      expect(store.compose()).toBeNull();
      expect(navigation.currentEntry.index).toBe(1);
      expect(navigation.entries().length).toBe(3);
    });

    it('closeCompose_AfterALoadedUrl_ReplacesTheUrl', async () => {
      const { store, router, navigation } = await create();
      store.registerComposeNames(['machine']);
      await router.navigateByUrl('/b');
      await router.navigateByUrl('/a?compose=machine:m1');
      const entryCount = navigation.entries().length;

      await store.closeCompose();

      expect(router.url).toBe('/a');
      expect(navigation.entries().length).toBe(entryCount);
      expect(navigation.currentEntry.index).toBe(2);
    });

    it('navigationEnd_WithAnUnknownName_RemovesCompose', async () => {
      const { store, router, navigation } = await create();
      store.registerComposeNames(['machine']);
      await router.navigateByUrl('/a');

      await router.navigateByUrl('/a?compose=bogus');

      await vi.waitFor(() => expect(router.url).toBe('/a'));
      expect(store.compose()).toBeNull();
      expect(navigation.entries().length).toBe(3);
    });

    it('navigationEnd_WithAKnownName_KeepsCompose', async () => {
      const { store, router } = await create();
      store.registerComposeNames(['machine']);

      await router.navigateByUrl('/a?compose=machine:m1');

      expect(router.url).toBe('/a?compose=machine:m1');
      expect(store.compose()).toBe('machine:m1');
    });

    it('registerComposeNames_AfterUnregister_NameIsUnknown', async () => {
      const { store, router } = await create();
      const unregister = store.registerComposeNames(['machine']);
      unregister();

      await router.navigateByUrl('/a?compose=machine:m1');

      await vi.waitFor(() => expect(router.url).toBe('/a'));
    });

    it('compose_AfterARedirectToNotFound_IsNull', async () => {
      const { store, router } = await create();
      store.registerComposeNames(['machine']);
      await router.navigateByUrl('/a?compose=machine:m1');

      await router.navigate(['/not-found'], { skipLocationChange: true });

      expect(store.compose()).toBeNull();
    });
  });
});
