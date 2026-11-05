import type { Routes } from '@angular/router';

import { HomeStore } from './home.store';
import { WelcomePage } from './welcome/welcome-page';

export const HOME_ROUTES: Routes = [{ path: '', component: WelcomePage, providers: [HomeStore] }];
