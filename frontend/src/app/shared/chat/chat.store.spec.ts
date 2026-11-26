import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';

import {
  type ChatMessage,
  ChatMessageRole,
  ChatMessageState,
  ChatState as ChatStateValue,
  type ChatTurn,
  type ChatTurnUpdate,
} from '@shared/chat/chat';
import { ChatStore } from '@shared/chat/chat.store';
import { STOP_RELOAD_DELAY_MILLISECONDS } from '@shared/chat/chat.store.utils';
import { provideFakeAuthStore } from '@shared/security/testing/fake-auth-store';
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

describe('ChatStore', { tags: ['Chat'] }, () => {
  const toast = vi.fn();
  const confirm = vi.fn();
  const ensureValidFor = vi.fn().mockResolvedValue(undefined);

  beforeEach(() => {
    toast.mockClear();
    confirm.mockClear();
    ensureValidFor.mockClear();
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
        { provide: ConfirmationService, useValue: { confirm } },
        provideFakeRealtimeConnection(connection),
        provideFakeAuthStore({ ensureValidFor }),
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

  function turn(chatId: string, userSequence: number): ChatTurn {
    return {
      chat: { id: chatId, title: 'T', state: ChatStateValue.Open, messageCount: userSequence + 1 },
      user: { ...message(userSequence), id: `u${userSequence}`, text: 'Question' },
      assistant: {
        ...message(userSequence + 1),
        id: `a${userSequence + 1}`,
        role: ChatMessageRole.Assistant,
        text: 'Answer',
      },
    };
  }

  async function openEmptyChat(store: InstanceType<typeof ChatStore>, http: HttpTestingController): Promise<void> {
    store.openChat('c1');
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 0 });
    http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [], nextBefore: null });
    await Promise.resolve();
  }

  it('send_WithAnOpenChat_AsksForAFreshTokenThenPostsTheMessage', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);

    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1/messages`);
    });

    expect(ensureValidFor).toHaveBeenCalledWith(90);
    expect(store.isSending()).toBe(true);
    expect(store.viewMessages().map((item) => item.text)).toEqual(['', 'Hello']);
  });

  it('send_WhenTheAnswerArrives_ReplacesThePendingMessagesWithTheTurn', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);
    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1/messages`).flush(turn('c1', 1));
    });

    expect(store.isSending()).toBe(false);
    expect(store.sends()).toEqual({});
    expect(store.messages().map((item) => item.text)).toEqual(['Answer', 'Question']);
  });

  it('send_WithoutAnOpenChat_CreatesTheChatAndNamesItForThePage', async () => {
    const { store, http } = setup();

    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(CHATS).flush(turn('new1', 1));
    });

    expect(store.createdChatId()).toBe('new1');
    expect(store.openChatId()).toBe('new1');
  });

  it('send_WhenTheServerAnswersTurnInProgress_ShowsTheStoppingToastAndKeepsNoPendingMessage', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);
    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1/messages`).flush(null, { status: 409, statusText: 'Conflict' });
    });

    expect(toast).toHaveBeenCalledWith(expect.objectContaining({ detail: 'CHAT.ERRORS.TURN_IN_PROGRESS' }));
    expect(store.sends()).toEqual({});
    expect(store.isSending()).toBe(false);
  });

  it('stop_WhileSending_SetsStoppingUntilTheEndPush', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);
    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1/messages`);
    });

    store.stop();
    const stoppingAfterStop = store.isStopping();
    store.applyEnd({ chatId: 'c1', messageId: 'a2', state: ChatMessageState.Cancelled });
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 2 });
    http
      .expectOne((request) => request.url === `${CHATS}/c1/messages`)
      .flush({ items: [message(2)], nextBefore: null });

    expect(stoppingAfterStop).toBe(true);
    expect(store.isStopping()).toBe(false);
    expect(store.isSending()).toBe(false);
  });

  it('stop_WhenNoEndPushComes_ReloadsAfterFiveSecondsAndEndsStopping', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);
    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1/messages`);
    });
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout'] });
    try {
      store.stop();

      vi.advanceTimersByTime(STOP_RELOAD_DELAY_MILLISECONDS);
      http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 2 });
      http
        .expectOne((request) => request.url === `${CHATS}/c1/messages`)
        .flush({ items: [message(2)], nextBefore: null });

      expect(store.isStopping()).toBe(false);
    } finally {
      vi.useRealTimers();
    }
  });

  it('requestDeleteMessage_AfterTheConfirmation_RemovesTheMessageAndItsReply', () => {
    const { store, http } = setup();
    store.openChat('c1');
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 3 });
    http
      .expectOne((request) => request.url === `${CHATS}/c1/messages`)
      .flush({ items: [message(3), message(2), message(1)], nextBefore: null });

    store.requestDeleteMessage(message(1));
    (confirm.mock.calls[0][0] as { accept: () => void }).accept();
    http.expectOne(`${CHATS}/c1/messages/m1`).flush(null);

    expect(store.messages().map((item) => item.sequence)).toEqual([3]);
  });

  it('requestDeleteChat_AfterTheConfirmation_DeletesTheChatAndClosesIt', () => {
    const { store, http } = setup();
    store.openChat('c1');
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 0 });
    http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [], nextBefore: null });

    store.requestDeleteChat();
    (confirm.mock.calls[0][0] as { accept: () => void }).accept();
    http.expectOne(`${CHATS}/c1`).flush(null);

    expect(store.deletedChatId()).toBe('c1');
    expect(store.openChatId()).toBeNull();
  });
  it('send_WhenAnotherChatIsOpenedDuringTheSend_KeepsTheOtherChatUnchangedWhenTheAnswerArrives', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);
    store.send('Hello', []);
    const sendRequest = await vi.waitFor(() => http.expectOne(`${CHATS}/c1/messages`));

    store.openChat('c2');
    http.expectOne(`${CHATS}/c2`).flush({ id: 'c2', title: 'Other', state: ChatStateValue.Open, messageCount: 0 });
    http.expectOne((request) => request.url === `${CHATS}/c2/messages`).flush({ items: [], nextBefore: null });
    const sendingInOther = store.isSending();
    const viewInOther = store.viewMessages();
    sendRequest.flush(turn('c1', 1));

    expect(sendingInOther).toBe(false);
    expect(viewInOther).toEqual([]);
    expect(store.openChatId()).toBe('c2');
    expect(store.messages()).toEqual([]);
    expect(store.chat()?.id).toBe('c2');
  });

  it('send_InASecondChatWhileTheFirstSends_PostsToTheSecondChat', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);
    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1/messages`);
    });
    store.openChat('c2');
    http.expectOne(`${CHATS}/c2`).flush({ id: 'c2', title: 'Other', state: ChatStateValue.Open, messageCount: 0 });
    http.expectOne((request) => request.url === `${CHATS}/c2/messages`).flush({ items: [], nextBefore: null });

    store.send('Hi', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c2/messages`);
    });

    expect(store.isSending()).toBe(true);
  });

  it('applyEnd_ForAnotherChatWhileStopping_KeepsTheStoppingState', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);
    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1/messages`);
    });
    store.stop();

    store.applyEnd({ chatId: 'other', messageId: 'a9', state: ChatMessageState.Completed });

    expect(store.isStopping()).toBe(true);
  });

  it('stop_WithoutASendOfThisTab_ChangesNoState', () => {
    const { store, http } = setup();
    store.openChat('c1');
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 0 });
    http.expectOne((request) => request.url === `${CHATS}/c1/messages`).flush({ items: [], nextBefore: null });

    store.stop();

    expect([store.isStopping(), store.isSending(), store.sends()]).toEqual([false, false, {}]);
  });

  it('send_WhenTheServerAnswersTurnInProgress_GivesTheTextBack', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);
    store.send('Keep this text', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1/messages`).flush(null, { status: 409, statusText: 'Conflict' });
    });

    expect(store.unsentText()).toBe('Keep this text');
  });

  it('send_ForANewChatWhileAnotherChatStreams_ShowsNoTextOfTheOtherChat', () => {
    const { store } = setup();
    store.applyUpdate({ chatId: 'chat-a', messageId: 'a1', sequence: 1, text: 'Text of chat A', activity: null });

    store.send('Hello', []);
    const beforeOwnPush = store.runningTurn();
    store.applyUpdate({ chatId: 'new-chat', messageId: 'n1', sequence: 1, text: 'Own text', activity: null });

    expect(beforeOwnPush).toBeNull();
    expect(store.runningTurn()?.text).toBe('Own text');
  });

  it('applyEnd_ForAnotherChatWhileANewChatSendStops_KeepsStoppingAndCreatesNoNavigation', () => {
    const { store } = setup();
    store.applyUpdate({ chatId: 'chat-a', messageId: 'a1', sequence: 1, text: 'Text of chat A', activity: null });
    store.send('Hello', []);
    store.stop();

    store.applyEnd({ chatId: 'chat-a', messageId: 'a1', state: ChatMessageState.Completed });

    expect(store.isStopping()).toBe(true);
    expect(store.createdChatId()).toBeNull();
  });

  it('viewMessages_AfterReopeningAChatWhileItsSendRuns_HoldsEachMessageOnce', async () => {
    const { store, http } = setup();
    await openEmptyChat(store, http);
    store.send('Hello', []);
    await vi.waitFor(() => {
      http.expectOne(`${CHATS}/c1/messages`);
    });
    store.openChat('c2');
    http.expectOne(`${CHATS}/c2`).flush({ id: 'c2', title: 'Other', state: ChatStateValue.Open, messageCount: 0 });
    http.expectOne((request) => request.url === `${CHATS}/c2/messages`).flush({ items: [], nextBefore: null });

    store.openChat('c1');
    http.expectOne(`${CHATS}/c1`).flush({ id: 'c1', title: 'T', state: ChatStateValue.Open, messageCount: 2 });
    http
      .expectOne((request) => request.url === `${CHATS}/c1/messages` && request.method === 'GET')
      .flush({
        items: [
          { ...message(2), id: 'a2', role: ChatMessageRole.Assistant, state: ChatMessageState.Streaming },
          { ...message(1), id: 'u1' },
        ],
        nextBefore: null,
      });
    store.applyUpdate({ chatId: 'c1', messageId: 'a2', sequence: 1, text: 'Part', activity: null });

    expect(store.viewMessages().map((item) => item.id)).toEqual(['a2', 'u1']);
  });
});
