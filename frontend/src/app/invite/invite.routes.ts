import type { Routes } from '@angular/router';

import { InviteApi } from '@invite/invite-api';
import { InviteLocation } from '@invite/invite-location';
import { InvitePage } from '@invite/invite-page/invite-page';
import { InviteStore } from '@invite/invite.store';

export const INVITE_ROUTES: Routes = [
  { path: '', title: 'TITLES.INVITE', component: InvitePage, providers: [InviteApi, InviteLocation, InviteStore] },
];
