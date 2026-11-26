import { Location, PlatformNavigation } from '@angular/common';
import type { SpyLocation } from '@angular/common/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { APP_ROUTER_FEATURES } from '@app/app-router';
import { NETWORK_ERROR_PATH, NETWORK_ERROR_URL, NOT_FOUND_URL } from '@shared/http/fallback-urls';
import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakeRealtimeConnection } from '@shared/realtime/testing/fake-realtime-connection';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
import { CompanyStore } from '@shared/company/company.store';

import { NetworkErrorPage } from '@fallback/network-error/network-error-page';
import { FALLBACK_ROUTES } from '@fallback/fallback.routes';
import { NotFoundPage } from '@fallback/not-found/not-found-page';
import { MAINTENANCE_ROUTES } from '@maintenance/maintenance.routes';

const CASE_PATH = '/maintenance/cases/c1';
const CASE_URL = '/api/insights/v1/cases/c1';

@Component({ template: '', selector: 'hf-start-stub', host: { class: 'contents' } })
class StartStub {}

function setup(addressBarPath = '/'): {
  http: HttpTestingController;
  router: Router;
  navigation: FakePlatformNavigation;
} {
  TestBed.configureTestingModule({
    providers: [
      provideRouter(
        [
          { path: NETWORK_ERROR_PATH, component: NetworkErrorPage },
          { path: 'start', component: StartStub },
          { path: 'maintenance', children: MAINTENANCE_ROUTES },
          { path: '', children: FALLBACK_ROUTES },
        ],
        ...APP_ROUTER_FEATURES,
      ),
      provideFakePlatformNavigation(),
      provideFakeRealtimeConnection(),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      provideFakeAuthStore({ hasPermission: () => true }),
      { provide: CompanyStore, useValue: { timeZoneId: () => 'Europe/Kyiv' } },
      { provide: MessageService, useValue: { add: vi.fn() } },
    ],
  });
  (TestBed.inject(Location) as SpyLocation).setInitialPath(addressBarPath);
  return {
    http: TestBed.inject(HttpTestingController),
    router: TestBed.inject(Router),
    navigation: TestBed.inject(PlatformNavigation) as FakePlatformNavigation,
  };
}

function activeComponent(router: Router): unknown {
  let route = router.routerState.snapshot.root;
  while (route.firstChild !== null) {
    route = route.firstChild;
  }
  return route.component;
}

describe('fallback routes', { tags: ['Platform'] }, () => {
  beforeEach(() => {
    vi.stubGlobal(
      'ResizeObserver',
      class {
        observe = vi.fn();
        unobserve = vi.fn();
        disconnect = vi.fn();
      },
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('reload_OfANetworkFallbackEntry_RequestsTheUrlAgain', async () => {
    const { http, router, navigation } = setup(CASE_PATH);
    await RouterTestingHarness.create();
    await router.navigateByUrl(NETWORK_ERROR_URL, { browserUrl: CASE_PATH });
    const entryCount = navigation.entries().length;

    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush({ id: 'c1', machineId: 'm1', state: 'open', signalCount: 0 });
    });
    await vi.waitFor(() => {
      expect(router.url).toBe(CASE_PATH);
    });

    expect(navigation.entries().length).toBe(entryCount);
  });

  it('reload_OfANotFoundEntry_WithTheItemStillMissing_ShowsNotFoundOnce', async () => {
    const { http, router, navigation } = setup(CASE_PATH);
    await RouterTestingHarness.create();
    await router.navigateByUrl(NOT_FOUND_URL, { browserUrl: CASE_PATH });

    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush(null, { status: 404, statusText: 'Not Found' });
    });
    await vi.waitFor(() => {
      expect(router.url).toBe(NOT_FOUND_URL);
      expect(navigation.currentEntry.url).toContain(CASE_PATH);
    });

    http.expectNone(CASE_URL);
    expect(activeComponent(router)).toBe(NotFoundPage);
  });

  it('firstLoad_OfAMissingItem_ShowsNotFoundAfterOneRequest', async () => {
    const { http, router } = setup(CASE_PATH);
    const harness = await RouterTestingHarness.create();

    const opening = harness.navigateByUrl(CASE_PATH);
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).flush(null, { status: 404, statusText: 'Not Found' });
    });
    await opening;

    expect(activeComponent(router)).toBe(NotFoundPage);
    http.expectNone(CASE_URL);
  });

  it('inAppFallback_DoesNotRetryByItself', async () => {
    const { http, router } = setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/start');

    const opening = harness.navigateByUrl(CASE_PATH);
    await vi.waitFor(() => {
      http.expectOne(CASE_URL).error(new ProgressEvent('error'), { status: 0 });
    });
    await opening;

    expect(router.url).toBe(NETWORK_ERROR_URL);
    http.expectNone(CASE_URL);
  });
});
