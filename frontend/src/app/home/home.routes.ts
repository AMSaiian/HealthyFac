import type { Routes } from '@angular/router';

import { HomeStore } from './home.store';
import { Welcome } from './welcome/welcome';

export const HOME_ROUTES: Routes = [{ path: '', component: Welcome, providers: [HomeStore] }];
