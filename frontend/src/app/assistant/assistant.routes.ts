import type { Routes } from '@angular/router';

import { ChatsListStore } from '@assistant/chats-list.store';
import { ChatsPage } from '@assistant/chats-page/chats-page';

export const ASSISTANT_ROUTES: Routes = [
  {
    path: '',
    title: 'TITLES.ASSISTANT',
    providers: [ChatsListStore],
    children: [
      { path: '', component: ChatsPage },
      { path: ':chatId', component: ChatsPage },
    ],
  },
];
