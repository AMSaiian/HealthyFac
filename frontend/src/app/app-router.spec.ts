import { Location, PlatformNavigation } from '@angular/common';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router, type Routes } from '@angular/router';

import { APP_ROUTER_FEATURES } from './app-router';

import { NETWORK_ERROR_PATH } from '@shared/http/fallback-urls';

import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';

@Component({ template: '', host: { class: 'contents' } })
class Blank {}

@Component({ template: '', host: { class: 'contents' } })
class Network {}

const CHUNK_ERROR_MESSAGE = 'Failed to fetch dynamically imported module: x';

function setupLazy(loadComponent: () => Promise<typeof Blank>): {
  router: Router;
  navigation: FakePlatformNavigation;
} {
  const routes: Routes = [
    { path: NETWORK_ERROR_PATH, component: Network },
    { path: '', component: Blank },
    { path: 'lazy', loadComponent },
  ];
  TestBed.configureTestingModule({
    providers: [provideRouter(routes, ...APP_ROUTER_FEATURES), provideFakePlatformNavigation()],
  });
  const router = TestBed.inject(Router);
  router.initialNavigation();
  return { router, navigation: TestBed.inject(PlatformNavigation) as FakePlatformNavigation };
}

describe('APP_ROUTER_FEATURES', () => {
  it('cancelledBrowserBack_WithComputedResolution_KeepsTheEntries', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(
          [
            { path: '', component: Blank },
            { path: 'a', component: Blank },
            { path: 'b', component: Blank, canDeactivate: [() => false] },
          ],
          ...APP_ROUTER_FEATURES,
        ),
        provideFakePlatformNavigation(),
      ],
    });
    const router = TestBed.inject(Router);
    const navigation = TestBed.inject(PlatformNavigation) as FakePlatformNavigation;
    router.initialNavigation();
    await router.navigateByUrl('/a');
    await router.navigateByUrl('/b');
    const urlsBefore = navigation.entries().map((entry) => entry.url);
    const indexBefore = navigation.currentEntry.index;

    TestBed.inject(Location).back();
    await vi.waitFor(() => expect(navigation.currentEntry.index).toBe(indexBefore));

    expect(navigation.entries().map((entry) => entry.url)).toEqual(urlsBefore);
    expect(router.url).toBe('/b');
  });

  it('chunkLoadFailure_ShowsTheNetworkPageWithTheRequestedUrl', async () => {
    const { router, navigation } = setupLazy(() => Promise.reject(new TypeError(CHUNK_ERROR_MESSAGE)));
    await router.navigateByUrl('/');

    await router.navigateByUrl('/lazy');

    expect(router.url).toBe('/' + NETWORK_ERROR_PATH);
    expect(navigation.currentEntry.url).toContain('/lazy');
  });

  it('otherNavigationError_KeepsItsHandling', async () => {
    const { router } = setupLazy(() => Promise.reject(new TypeError('something else failed')));
    await router.navigateByUrl('/');

    await expect(router.navigateByUrl('/lazy')).rejects.toThrow('something else failed');
    expect(router.url).toBe('/');
  });

  it('retry_AfterTheChunkLoads_ActivatesTheTarget', async () => {
    const loads = vi
      .fn<() => Promise<typeof Blank>>()
      .mockRejectedValueOnce(new TypeError(CHUNK_ERROR_MESSAGE))
      .mockResolvedValue(Blank);
    const { router, navigation } = setupLazy(loads);
    await router.navigateByUrl('/');
    await router.navigateByUrl('/lazy');
    const entryCount = navigation.entries().length;

    await router.navigateByUrl(TestBed.inject(Location).path());

    expect(router.url).toBe('/lazy');
    expect(loads).toHaveBeenCalledTimes(2);
    expect(navigation.entries().length).toBe(entryCount);
  });
});
