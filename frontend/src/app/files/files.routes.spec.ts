import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { AuthStore } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { FILES_ROUTES } from '@files/files.routes';
import { CompanyFilesPage } from '@files/list/company-files-page/company-files-page';
import { FilesGridStore } from '@files/list/files-grid.store';

const SEARCH_URL = '/api/document/v1/files/search';

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
});
