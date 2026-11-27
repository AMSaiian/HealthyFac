import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ChatLinkKind } from '@shared/chat/chat';
import { ChatApi } from '@shared/chat/chat-api';

const BASE = '/api/insights/v1/chats';

describe('ChatApi', { tags: ['Chat'] }, () => {
  function setup(): { api: ChatApi; http: HttpTestingController } {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    return { api: TestBed.inject(ChatApi), http: TestBed.inject(HttpTestingController) };
  }

  it('create_WithABody_PostsToTheChatsRoute', () => {
    const { api, http } = setup();
    const body = { text: 'Hi', links: [{ kind: ChatLinkKind.Machine, entityId: 'm1', position: 0 }] };

    api.create(body).subscribe();
    const request = http.expectOne(BASE);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(body);
  });

  it('send_WithAChatId_PostsToItsMessagesRoute', () => {
    const { api, http } = setup();

    api.send('c 1', { text: 'Hi', links: [] }).subscribe();
    const request = http.expectOne(`${BASE}/c%201/messages`);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ text: 'Hi', links: [] });
  });

  it('search_WithABody_PostsToTheSearchRoute', () => {
    const { api, http } = setup();

    api.search({ limit: 20, filter: {} }).subscribe();
    const request = http.expectOne(`${BASE}/search`);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ limit: 20, filter: {} });
  });

  it('get_WithAChatId_GetsTheChat', () => {
    const { api, http } = setup();

    api.get('c1').subscribe();

    expect(http.expectOne(`${BASE}/c1`).request.method).toBe('GET');
  });

  it('messages_WithBefore_SendsLimitAndBeforeAsQuery', () => {
    const { api, http } = setup();

    api.messages('c1', 30, 12).subscribe();
    const request = http.expectOne((candidate) => candidate.url === `${BASE}/c1/messages`);

    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('limit')).toBe('30');
    expect(request.request.params.get('before')).toBe('12');
  });

  it('messages_WithNoBefore_SendsOnlyTheLimit', () => {
    const { api, http } = setup();

    api.messages('c1', 30).subscribe();
    const request = http.expectOne((candidate) => candidate.url === `${BASE}/c1/messages`);

    expect(request.request.params.has('before')).toBe(false);
  });

  it('delete_WithAChatId_DeletesTheChat', () => {
    const { api, http } = setup();

    api.delete('c1').subscribe();

    expect(http.expectOne(`${BASE}/c1`).request.method).toBe('DELETE');
  });

  it('deleteMessage_WithIds_DeletesTheMessageRoute', () => {
    const { api, http } = setup();

    api.deleteMessage('c1', 'm1').subscribe();

    expect(http.expectOne(`${BASE}/c1/messages/m1`).request.method).toBe('DELETE');
  });
});
