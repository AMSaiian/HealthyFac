import { Location } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import { APP_ROUTER_FEATURES } from '@app/app-router';
import { ASSISTANT_ROUTES } from '@assistant/assistant.routes';
import { ChatsPage } from '@assistant/chats-page/chats-page';
import { ChatState } from '@shared/chat/chat';
import { ChatStore } from '@shared/chat/chat.store';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakeRealtimeConnection } from '@shared/realtime/testing/fake-realtime-connection';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';

const CHATS = '/api/insights/v1/chats';
const EMPTY_PAGE = { items: [], totalCount: 0, nextPosition: null };

function chat(id: string): { id: string; title: string; state: ChatState; messageCount: number } {
  return { id, title: `Chat ${id}`, state: ChatState.Open, messageCount: 1 };
}

function setup(): HttpTestingController {
  TestBed.configureTestingModule({
    providers: [
      provideFakeRealtimeConnection(),
      provideRouter([{ path: 'assistant', children: ASSISTANT_ROUTES }], ...APP_ROUTER_FEATURES),
      provideFakePlatformNavigation(),
      provideHttpClient(),
      provideHttpClientTesting(),
      provideTranslateService(),
      provideFakeAuthStore({ permissions: [] }),
      { provide: MessageService, useValue: { add: vi.fn() } },
    ],
  });
  return TestBed.inject(HttpTestingController);
}

async function openAssistant(url: string, http: HttpTestingController): Promise<RouterTestingHarness> {
  const harness = await RouterTestingHarness.create();
  await harness.navigateByUrl(url);
  await vi.waitFor(() => {
    http.expectOne(`${CHATS}/search`).flush(EMPTY_PAGE);
  });
  return harness;
}

describe('assistant routes', () => {
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

  it('assistantRoute_WithNoChatId_SearchesTheChats', async () => {
    const http = setup();

    const harness = await openAssistant('/assistant', http);

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(ChatsPage);
    expect(TestBed.inject(ChatStore).openChatId()).toBeNull();
  });

  it('assistantRoute_WithAChatId_LoadsTheChatAndItsFirstMessagePage', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/assistant/c1');
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/search`).flush(EMPTY_PAGE);
      http.expectOne(`${CHATS}/c1`).flush(chat('c1'));
      http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [], nextBefore: null });
    });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(ChatsPage);
    expect(TestBed.inject(ChatStore).chat()?.id).toBe('c1');
  });

  it('assistantRoute_WithAnUnknownChatId_EndsInTheNotFoundState', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/assistant/missing');
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/search`).flush(EMPTY_PAGE);
      http.expectOne(`${CHATS}/missing`).flush(null, { status: 404, statusText: 'Not Found' });
    });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(ChatsPage);
    expect(TestBed.inject(ChatStore).isNotFound()).toBe(true);
  });

  it('assistantRoute_BackFromASecondChat_OpensTheFirstChatAgain', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/assistant/c1');
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/search`).flush(EMPTY_PAGE);
      http.expectOne(`${CHATS}/c1`).flush(chat('c1'));
      http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [], nextBefore: null });
    });
    await harness.navigateByUrl('/assistant/c2');
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c2`).flush(chat('c2'));
      http.expectOne((request) => request.url === `${CHATS}/c2/messages`).flush({ items: [], nextBefore: null });
    });

    TestBed.inject(Location).back();
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1`).flush(chat('c1'));
      http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [], nextBefore: null });
    });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(ChatsPage);
    expect(TestBed.inject(ChatStore).openChatId()).toBe('c1');
  });
});
