import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';

import {
  type ChatMessage,
  ChatMessageRole,
  ChatMessageState,
  ChatState as ChatStateValue,
  type ChatTurnUpdate,
} from '@shared/chat/chat';
import { ChatStore } from '@shared/chat/chat.store';
import {
  createFakeRealtimeConnection,
  provideFakeRealtimeConnection,
} from '@shared/realtime/testing/fake-realtime-connection';

const CHATS = '/api/insights/v1/chats';

function message(sequence: number): ChatMessage {
  return {
    id: `m${sequence}`,
    sequence,
    role: ChatMessageRole.User,
    kind: null,
    state: ChatMessageState.Completed,
    text: `t${sequence}`,
    errorCode: null,
    links: [],
    drafts: [],
  };
}

function update(sequence: number, text: string): ChatTurnUpdate {
  return { chatId: 'c1', messageId: 'a1', sequence, text, activity: null };
}

describe('ChatStore', () => {
  const toast = vi.fn();

  beforeEach(() => {
    toast.mockClear();
  });

  function setup(): {
    store: InstanceType<typeof ChatStore>;
    http: HttpTestingController;
    connection: ReturnType<typeof createFakeRealtimeConnection>;
  } {
    const connection = createFakeRealtimeConnection();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideTranslateService(),
        { provide: MessageService, useValue: { add: toast } },
        provideFakeRealtimeConnection(connection),
      ],
    });
    return { store: TestBed.inject(ChatStore), http: TestBed.inject(HttpTestingController), connection };
  }

  it('applyUpdate_WithAStaleSequence_ChangesNothing', () => {
    const { store } = setup();
    store.applyUpdate(update(3, 'new'));

    store.applyUpdate(update(2, 'old'));

    expect(store.running()['c1'].text).toBe('new');
  });

  it('applyUpdate_FromTheRealtimeConnection_StoresTheRunningTurn', () => {
    const { store, connection } = setup();

    connection.chatUpdates$.next(update(1, 'Hi'));

    expect(store.running()['c1'].text).toBe('Hi');
  });

  it('applyEnd_ForAChat_ClearsItsRunningTurn', () => {
    const { store } = setup();
    store.applyUpdate(update(1, 'Hi'));

    store.applyEnd({ chatId: 'c1', messageId: 'a1', state: ChatMessageState.Completed });

    expect(store.running()['c1']).toBeUndefined();
  });

  it('loadOlder_AfterTheOldestPage_SendsNoRequest', () => {
    const { store, http } = setup();
    store.openChat('c1');
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 1 });
    http
      .expectOne((request) => request.url === `${CHATS}/c1/messages`)
      .flush({ items: [message(1)], nextBefore: null });

    store.loadOlder();

    http.expectNone((request) => request.url === `${CHATS}/c1/messages`);
    expect(store.messages().length).toBe(1);
  });

  it('loadOlder_WithANextBefore_AppendsTheOlderPage', () => {
    const { store, http } = setup();
    store.openChat('c1');
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 2 });
    http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [message(2)], nextBefore: 2 });

    store.loadOlder();
    const request = http.expectOne((candidate) => candidate.url === `${CHATS}/c1/messages`);
    request.flush({ items: [message(1)], nextBefore: null });

    expect(request.request.params.get('before')).toBe('2');
    expect(store.messages().map((item) => item.sequence)).toEqual([2, 1]);
  });

  it('openChat_WithAnUnknownId_SetsTheNotFoundState', () => {
    const { store, http } = setup();

    store.openChat('missing');
    http.expectOne(`${CHATS}/missing`).flush(null, { status: 404, statusText: 'Not Found' });
    http.match((request) => request.url === `${CHATS}/missing/messages`);

    expect(store.isNotFound()).toBe(true);
  });

  it('openChat_WhenTheMessagesAnswerNotFoundFirst_ShowsNoToastAndTheNotFoundState', () => {
    const { store, http } = setup();

    store.openChat('missing');
    http
      .expectOne((request) => request.url === `${CHATS}/missing/messages`)
      .flush(null, { status: 404, statusText: 'Not Found' });

    expect(store.isNotFound()).toBe(true);
    expect(toast).not.toHaveBeenCalled();
  });

  it('openChat_WhenTheLoadFailsWithAServerError_ShowsAnErrorToastAndNoNotFoundState', () => {
    const { store, http } = setup();

    store.openChat('c1');
    http.expectOne(`${CHATS}/c1`).flush(null, { status: 500, statusText: 'Server Error' });

    expect(toast).toHaveBeenCalledTimes(1);
    expect(store.isNotFound()).toBe(false);
  });

  it('openChat_WhenTheChatChangesBeforeTheFirstAnswer_KeepsTheLoadingFlag', () => {
    const { store, http } = setup();
    store.openChat('a');
    store.openChat('b');

    expect(store.isLoading()).toBe(true);
    http.expectOne(`${CHATS}/b`);
  });

  it('loadOlder_AfterAChatSwitchDuringAnOlderLoad_SendsTheRequestOfTheNewChat', () => {
    const { store, http } = setup();
    store.openChat('a');
    http.expectOne(`${CHATS}/a`).flush({ id: 'a', title: 'A', state: ChatStateValue.Open, messageCount: 2 });
    http.expectOne((request) => request.url === `${CHATS}/a/messages`).flush({ items: [message(2)], nextBefore: 2 });
    store.loadOlder();
    store.openChat('b');
    http.expectOne(`${CHATS}/b`).flush({ id: 'b', title: 'B', state: ChatStateValue.Open, messageCount: 2 });
    http.expectOne((request) => request.url === `${CHATS}/b/messages`).flush({ items: [message(2)], nextBefore: 2 });

    store.loadOlder();

    http.expectOne((request) => request.url === `${CHATS}/b/messages` && request.params.get('before') === '2');
  });

  it('applyEnd_ForTheOpenChat_KeepsTheOlderPageAndPutsTheNewReplyFirst', () => {
    const { store, http } = setup();
    store.openChat('c1');
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 2 });
    http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [message(2)], nextBefore: 2 });
    store.loadOlder();
    http
      .expectOne((request) => request.url === `${CHATS}/c1/messages`)
      .flush({ items: [message(1)], nextBefore: null });

    store.applyEnd({ chatId: 'c1', messageId: 'm3', state: ChatMessageState.Completed });
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 3 });
    http
      .expectOne((request) => request.url === `${CHATS}/c1/messages`)
      .flush({ items: [message(3), message(2)], nextBefore: 2 });

    expect(store.messages().map((item) => item.sequence)).toEqual([3, 2, 1]);
    expect(store.chat()?.messageCount).toBe(3);
  });
});
