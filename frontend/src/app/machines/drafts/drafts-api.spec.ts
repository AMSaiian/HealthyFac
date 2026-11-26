import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { DraftsApi } from '@machines/drafts/drafts-api';
import type { OperatingPhaseBody } from '@machines/phases/operating-phase';

describe('DraftsApi', { tags: ['Drafts'] }, () => {
  it('applyPhaseDraft_Always_PostsToTheTelemetryMachinesNotToCore', () => {
    TestBed.configureTestingModule({ providers: [DraftsApi, provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);
    const body = { code: 'cutting' } as OperatingPhaseBody;

    TestBed.inject(DraftsApi).applyPhaseDraft('m1', 'ph', body).subscribe();

    const request = http.expectOne('/api/telemetry/v1/machines/m1/definition-drafts/operating-phases/ph/apply');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBe(body);
  });
});
