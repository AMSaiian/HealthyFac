import {
  type ChatActivity,
  type ChatLinkInput,
  type ChatMessage,
  ChatMessageRole,
  type ChatMessagesPage,
  ChatMessageState,
  type ChatTurn,
  type ChatTurnUpdate,
} from '@shared/chat/chat';

/** The seconds that the token must stay valid when a send starts, because a turn can last up to 90 seconds. */
export const SEND_TOKEN_SECONDS = 90;

/** The time that a stopping turn waits for the end push before the chat reloads. */
export const STOP_RELOAD_DELAY_MILLISECONDS = 5000;

/** The key of the send that creates a chat, because that chat has no ID yet. */
export const NEW_CHAT_KEY = 'new';

/** The send of one chat: the message that the request carries, and whether a stop waits for the end. */
export interface SendState {
  readonly text: string;
  readonly links: readonly ChatLinkInput[];
  readonly isStopping: boolean;
  /** The chats that were open or running when a new chat send started, so their pushes are not the new chat's. */
  readonly knownChatIds: readonly string[];
}

export const PENDING_USER_ID = 'pending-user';
const PENDING_ASSISTANT_ID = 'pending-assistant';

/** The number of messages that one page of a chat loads. */
export const CHAT_MESSAGES_PAGE_SIZE = 30;

/** The turn of a chat that streams now, as the push shows it. */
export interface RunningTurn {
  readonly messageId: string;
  readonly text: string;
  readonly sequence: number;
  readonly activity: ChatActivity | null;
}

/** Tells whether a push is older than the push that the store already applied for the same reply. */
export function isStaleUpdate(running: RunningTurn | undefined, update: ChatTurnUpdate): boolean {
  return running !== undefined && running.messageId === update.messageId && update.sequence <= running.sequence;
}

/**
 * Merges the newest page into the loaded messages (newest first): a message with the same ID is replaced, a new one
 * goes on top, and the older pages stay loaded. `nextBefore` stays when an older page is loaded, else it comes from the
 * page.
 */
export function mergeNewestPage(
  loaded: readonly ChatMessage[],
  page: ChatMessagesPage,
  nextBefore: number | null,
): { messages: readonly ChatMessage[]; nextBefore: number | null } {
  const pageIds = new Set(page.items.map((message) => message.id));
  const oldestInPage = Math.min(...page.items.map((message) => message.sequence));
  const hasOlderLoaded = page.items.length > 0 && loaded.some((message) => message.sequence < oldestInPage);
  return {
    messages: [...page.items, ...loaded.filter((message) => !pageIds.has(message.id))],
    nextBefore: hasOlderLoaded ? nextBefore : page.nextBefore,
  };
}

/** Adds an older page after the loaded messages (newest first) and drops a message that is loaded already. */
export function appendOlderPage(loaded: readonly ChatMessage[], page: ChatMessagesPage): readonly ChatMessage[] {
  const loadedIds = new Set(loaded.map((message) => message.id));
  return [...loaded, ...page.items.filter((message) => !loadedIds.has(message.id))];
}

/**
 * The messages that a send shows before the answer arrives, the newest first: the empty streaming reply, then the
 * message of the user. The reply takes the ID of the running turn, so the pushed text reaches it.
 */
export function pendingMessages(
  loaded: readonly ChatMessage[],
  pending: { readonly text: string; readonly links: readonly ChatLinkInput[] },
  runningMessageId: string | undefined,
): readonly ChatMessage[] {
  const nextSequence = (loaded[0]?.sequence ?? 0) + 1;
  return [
    {
      id: runningMessageId ?? PENDING_ASSISTANT_ID,
      sequence: nextSequence + 1,
      role: ChatMessageRole.Assistant,
      kind: null,
      state: ChatMessageState.Streaming,
      text: '',
      errorCode: null,
      links: [],
      drafts: [],
    },
    {
      id: PENDING_USER_ID,
      sequence: nextSequence,
      role: ChatMessageRole.User,
      kind: null,
      state: ChatMessageState.Completed,
      text: pending.text,
      errorCode: null,
      links: pending.links.map((link) => ({ ...link, label: '' })),
      drafts: [],
    },
  ];
}

/** Puts the answered turn on top of the loaded messages (newest first) and drops a message with the same ID. */
export function replaceWithTurn(loaded: readonly ChatMessage[], turn: ChatTurn): readonly ChatMessage[] {
  return [
    turn.assistant,
    turn.user,
    ...loaded.filter((message) => message.id !== turn.user.id && message.id !== turn.assistant.id),
  ];
}

/** Removes a user message and its reply, the message with the next sequence. */
export function removeMessageWithReply(loaded: readonly ChatMessage[], message: ChatMessage): readonly ChatMessage[] {
  return loaded.filter((candidate) => candidate.id !== message.id && candidate.sequence !== message.sequence + 1);
}

/** The sends without the one of the given key. */
export function withoutSend(
  sends: Readonly<Record<string, SendState>>,
  key: string,
): Readonly<Record<string, SendState>> {
  return Object.fromEntries(Object.entries(sends).filter(([sendKey]) => sendKey !== key));
}
