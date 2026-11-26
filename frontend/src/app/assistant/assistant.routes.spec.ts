import { Location } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import { APP_ROUTER_FEATURES } from '@app/app-router';
import { ASSISTANT_ROUTES } from '@assistant/assistant.routes';
import { ChatsPage } from '@assistant/chats-page/chats-page';
import { type ChatMessage, ChatMessageRole, ChatMessageState, ChatState } from '@shared/chat/chat';
import { ChatStore } from '@shared/chat/chat.store';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakeRealtimeConnection } from '@shared/realtime/testing/fake-realtime-connection';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';

const CHATS = '/api/insights/v1/chats';
const EMPTY_PAGE = { items: [], totalCount: 0, nextPosition: null };

function chat(id: string): { id: string; title: string; state: ChatState; messageCount: number } {
  return { id, title: `Chat ${id}`, state: ChatState.Open, messageCount: 1 };
}

const confirm = vi.fn();

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
      { provide: ConfirmationService, useValue: { confirm } },
    ],
  });
  return TestBed.inject(HttpTestingController);
}

function message(id: string, sequence: number, role: ChatMessageRole): ChatMessage {
  return {
    id,
    sequence,
    role,
    kind: null,
    state: ChatMessageState.Completed,
    text: id,
    errorCode: null,
    links: [],
    drafts: [],
  };
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
    confirm.mockClear();
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

  it('assistantRoute_AfterASendWithoutAChat_OpensTheCreatedChatAndReloadsTheList', async () => {
    const http = setup();
    const harness = await openAssistant('/assistant', http);
    const store = TestBed.inject(ChatStore);

    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(CHATS).flush({
        chat: chat('new1'),
        user: message('u1', 1, ChatMessageRole.User),
        assistant: message('a2', 2, ChatMessageRole.Assistant),
      });
    });
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/search`).flush(EMPTY_PAGE);
    });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(TestBed.inject(Router).url).toBe('/assistant/new1');
    expect(harness.routeDebugElement?.componentInstance).toBeInstanceOf(ChatsPage);
    expect(store.chat()?.id).toBe('new1');
    http.expectNone(`${CHATS}/new1`);
  });

  it('assistantRoute_AfterTheDeleteOfTheOpenChat_LeavesTheChatWithoutLoadingItAgain', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/assistant/c1');
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/search`).flush(EMPTY_PAGE);
      http.expectOne(`${CHATS}/c1`).flush(chat('c1'));
      http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [], nextBefore: null });
    });
    const store = TestBed.inject(ChatStore);

    store.requestDeleteChat();
    (confirm.mock.calls[0][0] as { accept: () => void }).accept();
    await vi.waitFor(() => {
      http.expectOne({ method: 'DELETE', url: `${CHATS}/c1` }).flush(null);
    });
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/search`).flush(EMPTY_PAGE);
    });
    await TestBed.inject(ApplicationRef).whenStable();

    expect(TestBed.inject(Router).url).toBe('/assistant');
    expect(store.openChatId()).toBeNull();
    http.expectNone(`${CHATS}/c1`);
  });

  it('assistantRoute_AfterAnEndedTurn_SearchesTheChatsExactlyOnce', async () => {
    const http = setup();
    await openAssistant('/assistant', http);

    TestBed.inject(ChatStore).applyEnd({ chatId: 'c1', messageId: 'a2', state: ChatMessageState.Completed });
    await TestBed.inject(ApplicationRef).whenStable();
    const requests = http.match(`${CHATS}/search`);
    requests[0].flush(EMPTY_PAGE);
    await TestBed.inject(ApplicationRef).whenStable();

    expect(requests).toHaveLength(1);
    http.expectNone(`${CHATS}/search`);
  });

  it('assistantRoute_OpeningAChatAfterADelete_SendsNoChatSearch', async () => {
    const http = setup();
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/assistant/c1');
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/search`).flush(EMPTY_PAGE);
      http.expectOne(`${CHATS}/c1`).flush(chat('c1'));
      http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [], nextBefore: null });
    });
    TestBed.inject(ChatStore).requestDeleteChat();
    (confirm.mock.calls[0][0] as { accept: () => void }).accept();
    await vi.waitFor(() => {
      http.expectOne({ method: 'DELETE', url: `${CHATS}/c1` }).flush(null);
    });
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/search`).flush(EMPTY_PAGE);
    });
    await TestBed.inject(ApplicationRef).whenStable();

    await harness.navigateByUrl('/assistant/c2');
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c2`).flush(chat('c2'));
      http.expectOne((request) => request.url === `${CHATS}/c2/messages`).flush({ items: [], nextBefore: null });
    });
    await TestBed.inject(ApplicationRef).whenStable();

    http.expectNone(`${CHATS}/search`);
    expect(TestBed.inject(ChatStore).openChatId()).toBe('c2');
  });
});
