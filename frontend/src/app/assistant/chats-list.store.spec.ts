import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ChatsListStore } from '@assistant/chats-list.store';
import { CHATS_PAGE_SIZE } from '@assistant/chats-list.store.utils';
import { type ChatListItem, ChatState } from '@shared/chat/chat';
import { toKeysetPosition } from '@shared/grid/requests/grid-request';

const SEARCH = '/api/insights/v1/chats/search';

function item(id: string): ChatListItem {
  return { id, title: id, state: ChatState.Open, updatedAt: '2026-01-01T00:00:00Z' };
}

function setup(): { store: InstanceType<typeof ChatsListStore>; http: HttpTestingController } {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), ChatsListStore],
  });
  return { store: TestBed.inject(ChatsListStore), http: TestBed.inject(HttpTestingController) };
}

describe('ChatsListStore', () => {
  it('loadNext_WithANextPosition_SendsItAsAfterAndAppendsThePage', () => {
    const { store, http } = setup();
    const position = toKeysetPosition({ key: 'p1' });
    http.expectOne(SEARCH).flush({ items: [item('a')], totalCount: 2, nextPosition: position });

    store.loadNext();
    const request = http.expectOne(SEARCH);
    request.flush({ items: [item('b')], totalCount: 2, nextPosition: null });

    expect(request.request.body).toEqual({ limit: CHATS_PAGE_SIZE, filter: {}, after: position });
    expect(store.items().map((row) => row.id)).toEqual(['a', 'b']);
  });

  it('loadNext_AfterTheLastPage_SendsNoRequest', () => {
    const { store, http } = setup();
    http.expectOne(SEARCH).flush({ items: [item('a')], totalCount: 1, nextPosition: null });

    store.loadNext();

    http.expectNone(SEARCH);
    expect(store.hasMore()).toBe(false);
  });

  it('loadNext_WhenTheFirstLoadFails_KeepsHasLoadedFalseSoNoEmptyStateShows', () => {
    const { store, http } = setup();

    http.expectOne(SEARCH).flush(null, { status: 500, statusText: 'Server Error' });

    expect(store.hasLoaded()).toBe(false);
    expect(store.items()).toEqual([]);
  });
});
