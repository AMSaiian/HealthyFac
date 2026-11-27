import { HttpClient, type HttpContext } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import type { Observable } from 'rxjs';

import type { Chat, ChatMessagesPage, ChatsPage, ChatTurn, SearchChatsBody, SendChatBody } from '@shared/chat/chat';

const CHATS_URL = '/api/insights/v1/chats';
const SEARCH_URL = `${CHATS_URL}/search`;
const MESSAGES_SEGMENT = 'messages';

interface RequestOptions {
  readonly context?: HttpContext;
}

function chatUrl(chatId: string): string {
  return `${CHATS_URL}/${encodeURIComponent(chatId)}`;
}

/** Owns every HTTP call of the assistant chats. */
@Injectable({ providedIn: 'root' })
export class ChatApi {
  private readonly _http = inject(HttpClient);

  /** Creates a chat with its first message. The answer arrives after the turn ends. */
  create(body: SendChatBody, options?: RequestOptions): Observable<ChatTurn> {
    return this._http.post<ChatTurn>(CHATS_URL, body, options);
  }

  /** Adds a message to the chat. The answer arrives after the turn ends. */
  send(chatId: string, body: SendChatBody, options?: RequestOptions): Observable<ChatTurn> {
    return this._http.post<ChatTurn>(`${chatUrl(chatId)}/${MESSAGES_SEGMENT}`, body, options);
  }

  search(body: SearchChatsBody): Observable<ChatsPage> {
    return this._http.post<ChatsPage>(SEARCH_URL, body);
  }

  get(chatId: string, options?: RequestOptions): Observable<Chat> {
    return this._http.get<Chat>(chatUrl(chatId), options);
  }

  /** One page of the messages, the newest first. Send the `nextBefore` of a page as `before` for the older one. */
  messages(chatId: string, limit: number, before?: number, options?: RequestOptions): Observable<ChatMessagesPage> {
    const params: Record<string, number> = before === undefined ? { limit } : { limit, before };
    return this._http.get<ChatMessagesPage>(`${chatUrl(chatId)}/${MESSAGES_SEGMENT}`, { params, ...options });
  }

  delete(chatId: string, options?: RequestOptions): Observable<void> {
    return this._http.delete<void>(chatUrl(chatId), options);
  }

  /** Deletes a user message and its reply. */
  deleteMessage(chatId: string, messageId: string, options?: RequestOptions): Observable<void> {
    return this._http.delete<void>(`${chatUrl(chatId)}/${MESSAGES_SEGMENT}/${encodeURIComponent(messageId)}`, options);
  }
}
