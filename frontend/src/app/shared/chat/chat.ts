import type { KeysetGridRequestBody, KeysetPage } from '@shared/grid/requests/grid-request';

export enum ChatState {
  Open = 'open',
  Blocked = 'blocked',
}

export enum ChatMessageRole {
  User = 'user',
  Assistant = 'assistant',
}

export enum ChatMessageKind {
  Answer = 'answer',
  Refusal = 'refusal',
  Unanswerable = 'unanswerable',
  BlockedNotice = 'blockedNotice',
}

export enum ChatMessageState {
  Streaming = 'streaming',
  Completed = 'completed',
  Cancelled = 'cancelled',
  Failed = 'failed',
  Interrupted = 'interrupted',
}

export enum ChatLinkKind {
  Machine = 'machine',
  MaintenanceCase = 'maintenanceCase',
  File = 'file',
  Employee = 'employee',
  WorkOrder = 'workOrder',
}

export enum ChatDraftKind {
  Machine = 'machine',
  Metric = 'metric',
  Status = 'status',
  OperatingPhase = 'operatingPhase',
  WorkOrder = 'workOrder',
}

export enum ChatActivity {
  Machines = 'machines',
  Signals = 'signals',
  Cases = 'cases',
  WorkOrders = 'workOrders',
  People = 'people',
  Schedule = 'schedule',
  Manuals = 'manuals',
  Files = 'files',
  Drafts = 'drafts',
}

export enum ChatErrorCode {
  ProviderUnavailable = 'providerUnavailable',
  TimeLimit = 'timeLimit',
  OutputLimit = 'outputLimit',
}

export interface ChatListItem {
  readonly id: string;
  readonly title: string;
  readonly state: ChatState;
  readonly updatedAt: string;
}

export interface Chat {
  readonly id: string;
  readonly title: string;
  readonly state: ChatState;
  readonly messageCount: number;
}

export interface ChatLink {
  readonly kind: ChatLinkKind;
  readonly entityId: string;
  readonly label: string;
  readonly position: number;
}

export interface ChatLinkInput {
  readonly kind: ChatLinkKind;
  readonly entityId: string;
  readonly position: number;
}

export interface ChatDraft {
  readonly kind: ChatDraftKind;
  readonly draftId: string;
  readonly title: string;
  readonly targetMachineId: string | null;
  readonly isCommitted: boolean;
}

export interface ChatMessage {
  readonly id: string;
  readonly sequence: number;
  readonly role: ChatMessageRole;
  readonly kind: ChatMessageKind | null;
  readonly state: ChatMessageState;
  readonly text: string;
  readonly errorCode: ChatErrorCode | null;
  readonly links: readonly ChatLink[];
  readonly drafts: readonly ChatDraft[];
}

export interface ChatMessagesPage {
  readonly items: readonly ChatMessage[];
  /** The `sequence` to send as `before` for the next older page, or `null` on the last page. */
  readonly nextBefore: number | null;
}

export interface ChatTurn {
  readonly chat: Chat;
  readonly user: ChatMessage;
  readonly assistant: ChatMessage;
}

export interface SendChatBody {
  readonly text: string;
  readonly links: readonly ChatLinkInput[];
}

export type ChatsPage = KeysetPage<ChatListItem>;

/** The column that the backend sorts the chat list by. It mirrors `ChatSortColumn` of the Insights service. */
export enum ChatSortColumn {
  UpdatedAt = 'updatedAt',
}

/** The chat list has no filter, so the filter is an empty object. */
export type SearchChatsBody = KeysetGridRequestBody<Record<string, never>, ChatSortColumn>;

/** The push of a running turn. A higher `sequence` replaces the text. */
export interface ChatTurnUpdate {
  readonly chatId: string;
  readonly messageId: string;
  readonly sequence: number;
  readonly text: string;
  readonly activity: ChatActivity | null;
}

export interface ChatTurnEnd {
  readonly chatId: string;
  readonly messageId: string;
  readonly state: ChatMessageState;
}
