import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { APP_ROUTER_FEATURES } from '@app/app-router';
import { APP_ACTION_HOSTS } from '@app/app.config';
import { provideActionHosts } from '@shared/actions/action-registry';
import { ActionStore } from '@shared/actions/action.store';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { FILES_ROUTES } from '@files/files.routes';
import { CompanyFilesPage } from '@files/list/company-files-page/company-files-page';
import { FilesGridStore } from '@files/list/files-grid.store';
import { FileActionType } from '@files/file-actions';
import { MACHINES_ROUTES } from '@machines/machines.routes';

const SEARCH_URL = '/api/document/v1/files/search';
const MACHINE_URL = '/api/core/v1/machines/m1';
const MACHINE = {
  id: 'm1',
  name: 'Lathe',
  serialNumber: 'S1',
  gatewayId: 'abcdefghijklmnop',
  state: 'operational',
  description: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: null,
};

class ResizeObserverStub {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}

describe('files routes', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('filesRoute_OnOpen_ActivatesTheCompanyPageAndSearchesNewestFirst', async () => {
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'files', children: FILES_ROUTES }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
        { provide: AuthStore, useValue: { hasPermission: () => true } },
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2026-01-01T00:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
      ],
    });
    const http = TestBed.inject(HttpTestingController);
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/files');
    TestBed.tick();

    await vi.waitFor(() => {
      expect(http.match(SEARCH_URL).length).toBeGreaterThan(0);
    });
    const store = harness.routeDebugElement?.injector.get(FilesGridStore);
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(CompanyFilesPage);
    expect(store?.scope()).toEqual({ kind: 'company' });
    expect(store?.sort()).toEqual({ column: 'uploadedAt', direction: 'desc' });
  });

  function configure(): HttpTestingController {
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
    TestBed.configureTestingModule({
      providers: [
        provideRouter(
          [
            { path: 'files', children: FILES_ROUTES },
            { path: 'machines', children: MACHINES_ROUTES },
          ],
          ...APP_ROUTER_FEATURES,
        ),
        provideFakePlatformNavigation(),
        provideActionHosts(APP_ACTION_HOSTS),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
        { provide: AuthStore, useValue: { hasPermission: () => true } },
        {
          provide: CLOCK,
          useValue: { now: () => Temporal.Instant.from('2026-01-01T00:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
        { provide: MessageService, useValue: { add: vi.fn() } },
        { provide: ConfirmationService, useValue: { confirm: vi.fn() } },
      ],
    });
    return TestBed.inject(HttpTestingController);
  }

  it('filesRoute_ComposeOnLoad_UsesTheCompanyScopeOnTheCompanyRoute', async () => {
    const http = configure();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/files?compose=file:f1');
    TestBed.tick();
    await vi.waitFor(() => expect(http.match(SEARCH_URL).length).toBeGreaterThan(0));

    await vi.waitFor(() =>
      expect(TestBed.inject(ActionStore).active()).toEqual({
        type: FileActionType.Edit,
        payload: { scope: { kind: 'company' }, id: 'f1' },
      }),
    );
  });

  it('filesRoute_ComposeOnLoad_UsesTheMachineScopeOnTheMachineRoute', async () => {
    const http = configure();
    const harness = await RouterTestingHarness.create();

    const navigation = harness.navigateByUrl('/machines/m1/files?compose=file:f1');
    await vi.waitFor(() => {
      http.expectOne(MACHINE_URL).flush(MACHINE);
    });
    await navigation;
    TestBed.tick();

    await vi.waitFor(() =>
      expect(TestBed.inject(ActionStore).active()).toEqual({
        type: FileActionType.Edit,
        payload: { scope: { kind: 'machine', machineId: 'm1' }, id: 'f1' },
      }),
    );
  });
});
