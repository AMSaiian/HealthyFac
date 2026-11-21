import { Location, PlatformNavigation } from '@angular/common';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';

import { APP_ROUTER_FEATURES } from './app-router';

import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';

@Component({ template: '', host: { class: 'contents' } })
class Blank {}

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
});
