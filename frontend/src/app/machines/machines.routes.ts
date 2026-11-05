import type { Routes } from '@angular/router';

import { MachinesGridStore } from './list/machines-grid.store';
import { MachinesPage } from './list/machines-page/machines-page';
import { MachinesApi } from './machines-api';

export const MACHINES_ROUTES: Routes = [
  { path: '', component: MachinesPage, providers: [MachinesApi, MachinesGridStore] },
];
