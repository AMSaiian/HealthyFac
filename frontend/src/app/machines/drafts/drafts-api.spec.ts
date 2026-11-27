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

  it('searchForChat_WithABody_PostsToTheCoreChatRoute', () => {
    TestBed.configureTestingModule({ providers: [DraftsApi, provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);

    TestBed.inject(DraftsApi).searchForChat('c 1', { limit: 5, filter: {} }).subscribe();

    const request = http.expectOne('/api/core/v1/chats/c%201/drafts/search');
    expect(request.request.method).toBe('POST');
  });

  it('listForScope_WithAChat_GetsTheCoreChatDrafts', () => {
    TestBed.configureTestingModule({ providers: [DraftsApi, provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);

    TestBed.inject(DraftsApi).listForScope({ kind: 'chat', id: 'c1' }).subscribe();

    expect(http.expectOne('/api/core/v1/chats/c1/drafts').request.method).toBe('GET');
  });

  it('listForScope_WithAnAttempt_GetsTheCoreAttemptDrafts', () => {
    TestBed.configureTestingModule({ providers: [DraftsApi, provideHttpClient(), provideHttpClientTesting()] });
    const http = TestBed.inject(HttpTestingController);

    TestBed.inject(DraftsApi).listForScope({ kind: 'attempt', id: 'a1' }).subscribe();

    expect(http.expectOne('/api/core/v1/ingest-attempts/a1/drafts').request.method).toBe('GET');
  });
});
